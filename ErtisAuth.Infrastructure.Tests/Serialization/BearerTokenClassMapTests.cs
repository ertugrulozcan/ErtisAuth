using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ErtisAuth.Infrastructure.Tests.Serialization;

/// <summary>
/// BearerToken is stored inside TokenCode (token code authorization) and must survive a MongoDB round-trip.
/// </summary>
public class BearerTokenClassMapTests
{
	#region Constructors
	
	public BearerTokenClassMapTests()
	{
		BearerTokenClassMap.Register();
	}
	
	#endregion
	
	#region Helpers
	
	private static TokenCode RoundTrip(BearerToken bearerToken)
	{
		var tokenCode = new TokenCode
		{
			MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00",
			Code = "ABC123",
			ExpiresIn = 300,
			CreatedAt = DateTime.UtcNow
		};
		
		tokenCode.AssignToken(bearerToken, "5f8a1b2c3d4e5f6a7b8c9d01");
		return BsonSerializer.Deserialize<TokenCode>(tokenCode.ToBsonDocument());
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public void RoundTrip_WithRefreshableToken_RestoresToken()
	{
		var createdAt = new DateTime(2026, 9, 28, 10, 30, 0, DateTimeKind.Utc);
		
		var token = RoundTrip(new BearerToken("access-token", TimeSpan.FromHours(1), "refresh-token", TimeSpan.FromHours(2), createdAt)).Token;
		
		Assert.NotNull(token);
		Assert.Equal("access-token", token.AccessToken);
		Assert.Equal("refresh-token", token.RefreshToken);
		Assert.Equal(3600, token.ExpiresInTimeStamp);
		Assert.Equal(TimeSpan.FromHours(2), token.RefreshExpiresIn);
		Assert.Equal(createdAt, token.CreatedAt);
		Assert.Equal(SupportedTokenTypes.Bearer, token.TokenType);
	}
	
	[Fact]
	public void RoundTrip_WithFreshToken_IsNotExpired()
	{
		var token = RoundTrip(new BearerToken("access-token", TimeSpan.FromHours(1), "refresh-token", TimeSpan.FromHours(2))).Token;
		
		Assert.NotNull(token);
		Assert.False(token.IsExpired);
	}
	
	[Fact]
	public void RoundTrip_WithoutRefreshToken_RestoresToken()
	{
		var token = RoundTrip(new BearerToken("access-token", TimeSpan.FromHours(1))).Token;
		
		Assert.NotNull(token);
		Assert.Equal("access-token", token.AccessToken);
		Assert.Null(token.RefreshToken);
	}
	
	[Fact]
	public void Serialize_WritesTokenTypeOnce()
	{
		var document = new BearerToken("access-token", TimeSpan.FromHours(1)).ToBsonDocument();
		
		Assert.Equal("Bearer", document["token_type"].AsString);
		Assert.Equal(1, document.Names.Count(x => x == "token_type"));
	}
	
	[Fact]
	public void Deserialize_WithDocumentWithoutCreatedAt_ReturnsExpiredToken()
	{
		// Shape of the tokens stored by master (BearerTokenDto)
		var document = BsonDocument.Parse("{ access_token: 'access-token', refresh_token: 'refresh-token', expires_in: 3600, refresh_token_expires_in: 7200 }");
		
		var token = BsonSerializer.Deserialize<BearerToken>(document);
		
		Assert.Equal("access-token", token.AccessToken);
		Assert.True(token.IsExpired);
	}
	
	#endregion
}