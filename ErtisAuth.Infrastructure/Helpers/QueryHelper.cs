using ErtisAuth.Core.Exceptions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ErtisAuth.Infrastructure.Helpers;

/// <summary>
/// Scopes caller supplied queries and aggregation pipelines to a membership before they reach the database.
/// Inputs are parsed with MongoDB.Bson, the same parser the driver uses for JsonFilterDefinition,
/// so that what is checked here is exactly what MongoDB executes. Anything that can not be scoped is rejected (fail closed).
/// </summary>
public static class QueryHelper
{
	#region Constants
	
	private const string MembershipIdField = "membership_id";
	
	/// <summary>
	/// Pipeline stages that only transform the documents flowing through the pipeline.
	/// Stages reading from or writing to other collections ($lookup, $graphLookup, $unionWith, $out, $merge, ...)
	/// or reading server state would escape the membership filter, so everything else is rejected.
	/// </summary>
	private static readonly HashSet<string> AllowedAggregationStages =
	[
		"$match",
		"$project",
		"$addFields",
		"$set",
		"$unset",
		"$group",
		"$sort",
		"$limit",
		"$skip",
		"$count",
		"$unwind",
		"$bucket",
		"$bucketAuto",
		"$sortByCount",
		"$replaceRoot",
		"$replaceWith",
		"$sample",
		"$setWindowFields",
		"$facet"
	];
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Returns the query as <c>{ "$and": [ { "membership_id": membershipId }, query ] }</c>.
	/// The caller's filter is kept as is; whatever it contains, only documents of the membership can match.
	/// </summary>
	// ReSharper disable once UnusedTypeParameter
	public static string InjectMembershipIdToQuery<TDto>(string query, string membershipId)
	{
		var filter = ParseQuery(query);
		var scopedFilter = new BsonDocument("$and", new BsonArray
		{
			new BsonDocument(MembershipIdField, membershipId),
			filter
		});
		
		return scopedFilter.ToJson();
	}
	
	/// <summary>
	/// Validates the pipeline stages against the allowlist and prepends <c>{ "$match": { "membership_id": membershipId } }</c>
	/// as the first stage, so that every following stage only sees documents of the membership.
	/// </summary>
	public static string InjectMembershipIdToAggregation(string pipeline, string membershipId)
	{
		var stages = ParsePipeline(pipeline);
		ValidateStages(stages);
		
		stages.Insert(0, new BsonDocument("$match", new BsonDocument(MembershipIdField, membershipId)));
		return stages.ToJson();
	}

	/// <summary>
	/// A case and diacritic insensitive text search within the membership:
	/// <c>{ "membership_id": membershipId, "$text": { "$search": keyword, ... } }</c>.
	/// Built as a document, so the keyword is always a string value and never parsed as query syntax.
	/// Without a language the text index's default language is used.
	/// </summary>
	public static string FullTextSearchQuery(string membershipId, string keyword, string? language = null)
	{
		var textSearch = new BsonDocument
		{
			{ "$search", keyword },
			{ "$caseSensitive", false },
			{ "$diacriticSensitive", false }
		};

		if (!string.IsNullOrEmpty(language))
		{
			textSearch.Add("$language", language);
		}

		return new BsonDocument
		{
			{ MembershipIdField, membershipId },
			{ "$text", textSearch }
		}.ToJson();
	}

	private static BsonDocument ParseQuery(string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return new BsonDocument();
		}
		
		try
		{
			return BsonDocument.Parse(query);
		}
		catch (Exception ex)
		{
			throw ErtisAuthException.InvalidQuery($"The query could not be parsed ({ex.Message})");
		}
	}
	
	private static BsonArray ParsePipeline(string pipeline)
	{
		if (string.IsNullOrWhiteSpace(pipeline))
		{
			throw ErtisAuthException.InvalidQuery("The aggregation pipeline is empty");
		}
		
		try
		{
			return BsonSerializer.Deserialize<BsonArray>(pipeline);
		}
		catch (Exception ex)
		{
			throw ErtisAuthException.InvalidQuery($"The aggregation pipeline could not be parsed ({ex.Message})");
		}
	}
	
	private static void ValidateStages(BsonArray stages)
	{
		foreach (var stage in stages)
		{
			if (stage is not BsonDocument { ElementCount: 1 } stageDocument)
			{
				throw ErtisAuthException.InvalidQuery("Each aggregation stage must be an object with a single stage operator");
			}
			
			var stageElement = stageDocument.GetElement(0);
			if (!AllowedAggregationStages.Contains(stageElement.Name))
			{
				throw ErtisAuthException.UnsupportedAggregationStage(stageElement.Name);
			}
			
			if (stageElement.Name == "$facet")
			{
				ValidateFacet(stageElement.Value);
			}
		}
	}
	
	private static void ValidateFacet(BsonValue facet)
	{
		if (facet is not BsonDocument facetDocument)
		{
			throw ErtisAuthException.InvalidQuery("The $facet stage must be an object of sub-pipelines");
		}
		
		foreach (var subPipeline in facetDocument)
		{
			if (subPipeline.Value is not BsonArray subStages)
			{
				throw ErtisAuthException.InvalidQuery("The $facet stage must be an object of sub-pipelines");
			}
			
			ValidateStages(subStages);
		}
	}
	
	#endregion
}
