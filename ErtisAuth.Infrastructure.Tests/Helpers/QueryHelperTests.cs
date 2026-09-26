using ErtisAuth.Core.Exceptions;
using ErtisAuth.Infrastructure.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

/// <summary>
/// Membership scoping of caller supplied queries (_query endpoints) and aggregation pipelines (_aggregate endpoints).
/// Results are read back with MongoDB.Bson, the parser MongoDB uses for these strings.
/// </summary>
public class QueryHelperTests
{
	#region Constants
	
	private const string OwnMembershipId = "membership-a";
	
	#endregion
	
	#region Helpers
	
	private static BsonDocument ScopeQuery(string query)
	{
		return BsonDocument.Parse(QueryHelper.InjectMembershipIdToQuery<dynamic>(query, OwnMembershipId));
	}
	
	private static BsonArray ScopePipeline(string pipeline)
	{
		return BsonSerializer.Deserialize<BsonArray>(QueryHelper.InjectMembershipIdToAggregation(pipeline, OwnMembershipId));
	}
	
	private static void AssertScopedQuery(BsonDocument scoped, BsonDocument expectedCallerFilter)
	{
		Assert.Equal(1, scoped.ElementCount);
		var and = scoped["$and"].AsBsonArray;
		Assert.Equal(2, and.Count);
		Assert.Equal(new BsonDocument("membership_id", OwnMembershipId), and[0]);
		Assert.Equal(expectedCallerFilter, and[1]);
	}
	
	private static void AssertStartsWithMembershipMatch(BsonArray stages)
	{
		Assert.Equal(new BsonDocument("$match", new BsonDocument("membership_id", OwnMembershipId)), stages[0]);
	}
	
	#endregion
	
	#region Query
	
	[Theory]
	[InlineData("{ \"gender\": \"female\" }")]
	[InlineData("{ \"membership_id\": \"membership-b\" }")]
	[InlineData("{ \"$or\": [ { \"membership_id\": \"membership-b\" }, { \"gender\": \"female\" } ] }")]
	[InlineData("{ \"where\": [] }")]
	[InlineData("{ \"where\": { \"membership_id\": \"membership-b\" } }")]
	[InlineData("{ \"$where\": \"true\" }")]
	public void InjectMembershipIdToQuery_AlwaysAndsTheMembershipConditionWithTheUntouchedCallerFilter(string query)
	{
		AssertScopedQuery(ScopeQuery(query), BsonDocument.Parse(query));
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	[InlineData("{}")]
	public void InjectMembershipIdToQuery_WithEmptyQuery_OnlyFiltersByMembership(string? query)
	{
		AssertScopedQuery(ScopeQuery(query!), new BsonDocument());
	}
	
	[Fact]
	public void InjectMembershipIdToQuery_SupportsMongoShellSyntax()
	{
		const string query = "{ \"_id\": ObjectId(\"5f8a1b2c3d4e5f6a7b8c9d0e\"), \"created_at\": { \"$gt\": ISODate(\"2020-01-01T00:00:00Z\") } }";
		
		AssertScopedQuery(ScopeQuery(query), BsonDocument.Parse(query));
	}
	
	[Theory]
	[InlineData("not json")]
	[InlineData("[]")]
	[InlineData("[ { \"membership_id\": \"membership-b\" } ]")]
	[InlineData("\"membership-b\"")]
	[InlineData("{ \"gender\": ")]
	public void InjectMembershipIdToQuery_WithUnparsableOrNonObjectQuery_ThrowsInvalidQuery(string query)
	{
		var exception = Assert.Throws<ErtisAuthException>(() => QueryHelper.InjectMembershipIdToQuery<dynamic>(query, OwnMembershipId));
		
		Assert.Equal("InvalidQuery", exception.ErrorCode);
	}
	
	#endregion
	
	#region Aggregation
	
	[Theory]
	[InlineData("[]")]
	[InlineData("[ { \"$group\": { \"_id\": \"$user_id\" } } ]")]
	[InlineData("[ { \"$match\": { \"membership_id\": \"membership-b\" } } ]")]
	[InlineData("[ { \"$addFields\": { \"membership_id\": \"membership-a\" } }, { \"$match\": { } } ]")]
	[InlineData("[ { \"$group\": { \"_id\": \"$membership_id\", \"count\": { \"$sum\": 1 } } }, { \"$match\": { } } ]")]
	public void InjectMembershipIdToAggregation_AlwaysPrependsTheMembershipMatchAndKeepsCallerStages(string pipeline)
	{
		var stages = ScopePipeline(pipeline);
		var callerStages = BsonSerializer.Deserialize<BsonArray>(pipeline);
		
		AssertStartsWithMembershipMatch(stages);
		Assert.Equal(callerStages, new BsonArray(stages.Skip(1)));
	}
	
	[Theory]
	[InlineData("$lookup", "{ \"from\": \"memberships\", \"pipeline\": [], \"as\": \"memberships\" }")]
	[InlineData("$graphLookup", "{ \"from\": \"users\", \"startWith\": \"$user_id\", \"connectFromField\": \"_id\", \"connectToField\": \"_id\", \"as\": \"users\" }")]
	[InlineData("$unionWith", "\"users\"")]
	[InlineData("$out", "\"roles\"")]
	[InlineData("$merge", "{ \"into\": \"users\" }")]
	[InlineData("$collStats", "{ }")]
	[InlineData("$indexStats", "{ }")]
	[InlineData("$currentOp", "{ }")]
	[InlineData("$documents", "[ { \"membership_id\": \"membership-b\" } ]")]
	[InlineData("$unknownStage", "{ }")]
	public void InjectMembershipIdToAggregation_WithStageOutsideAllowlist_ThrowsUnsupportedAggregationStage(string stage, string value)
	{
		var pipeline = $"[ {{ \"$match\": {{ }} }}, {{ \"{stage}\": {value} }} ]";
		
		var exception = Assert.Throws<ErtisAuthException>(() => QueryHelper.InjectMembershipIdToAggregation(pipeline, OwnMembershipId));
		
		Assert.Equal("UnsupportedAggregationStage", exception.ErrorCode);
	}
	
	[Fact]
	public void InjectMembershipIdToAggregation_WithDisallowedStageInsideFacet_ThrowsUnsupportedAggregationStage()
	{
		const string pipeline = "[ { \"$facet\": { \"leak\": [ { \"$lookup\": { \"from\": \"memberships\", \"pipeline\": [], \"as\": \"m\" } } ] } } ]";
		
		var exception = Assert.Throws<ErtisAuthException>(() => QueryHelper.InjectMembershipIdToAggregation(pipeline, OwnMembershipId));
		
		Assert.Equal("UnsupportedAggregationStage", exception.ErrorCode);
	}
	
	[Fact]
	public void InjectMembershipIdToAggregation_WithAllowedStagesInsideFacet_Succeeds()
	{
		const string pipeline = "[ { \"$facet\": { \"count\": [ { \"$count\": \"total\" } ], \"latest\": [ { \"$sort\": { \"created_at\": -1 } }, { \"$limit\": 5 } ] } } ]";
		
		AssertStartsWithMembershipMatch(ScopePipeline(pipeline));
	}
	
	[Fact]
	public void InjectMembershipIdToAggregation_SupportsMongoShellSyntax()
	{
		const string pipeline = "[ { \"$match\": { \"created_at\": { \"$gt\": ISODate(\"2020-01-01T00:00:00Z\") } } } ]";
		
		AssertStartsWithMembershipMatch(ScopePipeline(pipeline));
	}
	
	[Theory]
	[InlineData("")]
	[InlineData("not json")]
	[InlineData("{ \"$match\": { } }")]
	[InlineData("[ \"$match\" ]")]
	[InlineData("[ { \"$match\": { }, \"$limit\": 5 } ]")]
	[InlineData("[ { } ]")]
	[InlineData("[ { \"$facet\": [] } ]")]
	[InlineData("[ { \"$facet\": { \"x\": { } } } ]")]
	public void InjectMembershipIdToAggregation_WithMalformedPipeline_ThrowsInvalidQuery(string pipeline)
	{
		var exception = Assert.Throws<ErtisAuthException>(() => QueryHelper.InjectMembershipIdToAggregation(pipeline, OwnMembershipId));
		
		Assert.Equal("InvalidQuery", exception.ErrorCode);
	}
	
	[Fact]
	public void InjectMembershipIdToAggregation_OutputRoundTripsThroughTheTypedRepositoryParsing()
	{
		// Ertis.MongoDB's typed repository splits the pipeline with Newtonsoft (JArray.Parse) and parses each stage with BsonDocument.Parse.
		const string pipeline = "[ { \"$match\": { \"created_at\": { \"$gt\": ISODate(\"2020-01-01T00:00:00Z\") }, \"_id\": ObjectId(\"5f8a1b2c3d4e5f6a7b8c9d0e\") } }, { \"$group\": { \"_id\": \"$user_id\", \"count\": { \"$sum\": 1 } } } ]";
		var output = QueryHelper.InjectMembershipIdToAggregation(pipeline, OwnMembershipId);
		
		var stagesParsedLikeTheRepository = new BsonArray(Newtonsoft.Json.Linq.JArray.Parse(output).Select(x => BsonDocument.Parse(x.ToString())));
		
		Assert.Equal(ScopePipeline(pipeline), stagesParsedLikeTheRepository);
	}
	
	#endregion
}
