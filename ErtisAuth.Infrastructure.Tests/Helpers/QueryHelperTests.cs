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
	// The caller's own membership: redundant but harmless, results are unchanged
	[InlineData("{ \"membership_id\": \"membership-a\" }")]
	// membership_id conditions at inner levels or with operators
	[InlineData("{ \"membership_id\": { \"$in\": [ \"membership-a\", \"membership-b\" ] } }")]
	[InlineData("{ \"membership_id\": { \"$ne\": \"membership-a\" } }")]
	[InlineData("{ \"membership_id\": { \"$regex\": \".*\" } }")]
	[InlineData("{ \"membership_id\": { \"$exists\": false } }")]
	[InlineData("{ \"$nor\": [ { \"membership_id\": \"membership-a\" } ] }")]
	[InlineData("{ \"$expr\": { \"$eq\": [ \"$membership_id\", \"membership-b\" ] } }")]
	[InlineData("{ \"$and\": [ { \"$or\": [ { \"membership_id\": \"membership-b\" }, { \"$and\": [ { \"membership_id\": { \"$exists\": false } } ] } ] } ] }")]
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
	// The caller's own membership: redundant but harmless, results are unchanged
	[InlineData("[ { \"$match\": { \"membership_id\": \"membership-a\" } } ]")]
	// membership_id conditions at inner levels or with operators
	[InlineData("[ { \"$match\": { \"$or\": [ { \"membership_id\": \"membership-b\" }, { \"gender\": \"female\" } ] } } ]")]
	[InlineData("[ { \"$match\": { \"membership_id\": { \"$in\": [ \"membership-a\", \"membership-b\" ] } } } ]")]
	[InlineData("[ { \"$match\": { \"$expr\": { \"$ne\": [ \"$membership_id\", \"membership-a\" ] } } } ]")]
	[InlineData("[ { \"$facet\": { \"other\": [ { \"$match\": { \"membership_id\": \"membership-b\" } } ] } } ]")]
	[InlineData("[ { \"$set\": { \"membership_id\": \"membership-b\" } }, { \"$match\": { \"membership_id\": \"membership-b\" } } ]")]
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
	
	#region Hidden Fields And JavaScript
	
	private static readonly string[] HiddenFields = ["password_hash"];
	
	[Theory]
	[InlineData("""{ "password_hash": "x" }""")]
	[InlineData("""{ "password_hash": { "$regex": "^a" } }""")]
	[InlineData("""{ "password_hash.0": "x" }""")]
	[InlineData("""{ "profile.password_hash": "x" }""")]
	[InlineData("""{ "$and": [ { "username": "a" }, { "$or": [ { "password_hash": { "$exists": true } } ] } ] }""")]
	[InlineData("""{ "$nor": [ { "password_hash": { "$not": { "$regex": "^a" } } } ] }""")]
	[InlineData("""{ "$expr": { "$eq": [ "$username", "a" ] } }""")]
	[InlineData("""{ "$jsonSchema": { "required": [ "username" ] } }""")]
	public void InjectMembershipIdToQuery_ReachingAHiddenField_ThrowsInvalidQuery(string query)
	{
		var exception = Assert.Throws<ErtisAuthException>(() => QueryHelper.InjectMembershipIdToQuery<dynamic>(query, OwnMembershipId, HiddenFields));
		
		Assert.Equal("InvalidQuery", exception.ErrorCode);
	}
	
	[Theory]
	[InlineData("""{ "$where": "true" }""")]
	[InlineData("""{ "$where": function() { return true; } }""")]
	[InlineData("""{ "$and": [ { "$where": "true" } ] }""")]
	[InlineData("""{ "$expr": { "$function": { "body": "function() { return true; }", "args": [], "lang": "js" } } }""")]
	public void InjectMembershipIdToQuery_RunningJavaScript_ThrowsInvalidQuery(string query)
	{
		var exception = Assert.Throws<ErtisAuthException>(() => QueryHelper.InjectMembershipIdToQuery<dynamic>(query, OwnMembershipId));
		
		Assert.Equal("InvalidQuery", exception.ErrorCode);
	}
	
	[Theory]
	[InlineData("""{ "username": "admin" }""")]
	[InlineData("""{ "$or": [ { "username": "a" }, { "email_address": { "$regex": "@example" } } ] }""")]
	[InlineData("""{ "sys.created_at": { "$gt": "2026-01-01" } }""")]
	public void InjectMembershipIdToQuery_WithoutHiddenFields_IsScoped(string query)
	{
		var scoped = BsonDocument.Parse(QueryHelper.InjectMembershipIdToQuery<dynamic>(query, OwnMembershipId, HiddenFields));
		
		AssertScopedQuery(scoped, BsonDocument.Parse(query));
	}
	
	[Fact]
	public void InjectMembershipIdToQuery_OnResourceWithoutHiddenFields_AllowsExpr()
	{
		const string query = """{ "$expr": { "$eq": [ "$name", "admin" ] } }""";
		
		var scoped = BsonDocument.Parse(QueryHelper.InjectMembershipIdToQuery<dynamic>(query, OwnMembershipId));
		
		AssertScopedQuery(scoped, BsonDocument.Parse(query));
	}
	
	[Theory]
	[InlineData("""[ { "$match": { "$where": "true" } } ]""")]
	[InlineData("""[ { "$project": { "x": { "$function": { "body": "function() { return 1; }", "args": [], "lang": "js" } } } } ]""")]
	[InlineData("""[ { "$facet": { "a": [ { "$match": { "$where": "true" } } ] } } ]""")]
	public void InjectMembershipIdToAggregation_RunningJavaScript_ThrowsInvalidQuery(string pipeline)
	{
		var exception = Assert.Throws<ErtisAuthException>(() => QueryHelper.InjectMembershipIdToAggregation(pipeline, OwnMembershipId));
		
		Assert.Equal("InvalidQuery", exception.ErrorCode);
	}
	
	[Theory]
	[InlineData("password_hash", true)]
	[InlineData("-password_hash", true)]
	[InlineData("password_hash.x", true)]
	[InlineData("username", false)]
	[InlineData(null, false)]
	public void EnsureSortable_RejectsHiddenFields(string? sortField, bool rejected)
	{
		var exception = Record.Exception(() => QueryHelper.EnsureSortable(sortField, HiddenFields));
		
		Assert.Equal(rejected, exception is ErtisAuthException { ErrorCode: "InvalidQuery" });
	}
	
	#endregion
	
	#region Full Text Search
	
	[Theory]
	[InlineData("Editor")]
	[InlineData("a\"b")]
	[InlineData("back\\slash")]
	[InlineData("x\" } }, { \"membership_id\": { \"$ne\": \"\" } } ], \"$comment\": \"")]
	[InlineData("çğıöşü İ")]
	public void FullTextSearchQuery_KeepsTheKeywordAsAStringWithinTheMembership(string keyword)
	{
		var query = BsonDocument.Parse(QueryHelper.FullTextSearchQuery(OwnMembershipId, keyword));
		
		Assert.Equal(["membership_id", "$text"], query.Names);
		Assert.Equal(OwnMembershipId, query["membership_id"].AsString);
		
		var textSearch = query["$text"].AsBsonDocument;
		Assert.Equal(keyword, textSearch["$search"].AsString);
		Assert.False(textSearch["$caseSensitive"].AsBoolean);
		Assert.False(textSearch["$diacriticSensitive"].AsBoolean);
		Assert.False(textSearch.Contains("$language"));
	}
	
	[Fact]
	public void FullTextSearchQuery_WithLanguage_AddsTheLanguage()
	{
		var query = BsonDocument.Parse(QueryHelper.FullTextSearchQuery(OwnMembershipId, "editor", "tr"));
		
		Assert.Equal("tr", query["$text"]["$language"].AsString);
	}
	
	#endregion
}