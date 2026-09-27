using System.Reflection;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

/// <summary>
/// Expired token records are deleted by MongoDB TTL indexes; the fields they use must keep each record as long as it is needed.
/// </summary>
public class TokenRetentionTests
{
	#region Fields
	
	private static readonly MongoClient MongoClient = new("mongodb://localhost:27017");
	
	#endregion
	
	#region Helpers
	
	private static ActiveToken CreateActiveToken(int expiresIn, int refreshTokenExpiresIn)
	{
		return new ActiveToken
		{
			Id = "active-token-id",
			AccessToken = "access-token",
			RefreshToken = "refresh-token",
			ExpiresIn = expiresIn,
			RefreshTokenExpiresIn = refreshTokenExpiresIn,
			CreatedAt = new DateTime(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc),
			MembershipId = "membership-id"
		};
	}
	
	private static TTLIndexDefinition GetTTLIndex<TRepository>() where TRepository : class
	{
		// MongoClient connects lazily, on the first operation; building a repository does not reach any server
		var clientProvider = Substitute.For<IMongoClientProvider>();
		clientProvider.Client.Returns(MongoClient);
		var settings = Substitute.For<IDatabaseSettings>();
		settings.DefaultAuthDatabase.Returns("ertisauth-tests");
		var repository = (TRepository) Activator.CreateInstance(
			typeof(TRepository),
			clientProvider,
			settings,
			NullLogger<TRepository>.Instance)!;
		
		var indexes = (IIndexDefinition[]) typeof(TRepository)
			.GetProperty("Indexes", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(repository)!;
		
		return Assert.Single(indexes.OfType<TTLIndexDefinition>());
	}
	
	#endregion
	
	#region Active Tokens
	
	[Fact]
	public void ActiveToken_RetainUntil_CoversTheRefreshToken()
	{
		var activeToken = CreateActiveToken(expiresIn: 3600, refreshTokenExpiresIn: 86400);
		
		Assert.Equal(activeToken.CreatedAt.AddSeconds(86400), activeToken.RetainUntil);
	}
	
	[Fact]
	public void ActiveToken_RetainUntil_CoversTheAccessTokenEvenWithShorterRefreshToken()
	{
		// Misconfigured membership: deleting by the refresh token expiry would make the access token unrevocable
		var activeToken = CreateActiveToken(expiresIn: 7200, refreshTokenExpiresIn: 3600);
		
		Assert.Equal(activeToken.CreatedAt.AddSeconds(7200), activeToken.RetainUntil);
	}
	
	[Fact]
	public void ActiveToken_RefreshTokenExpireTime_IsCreationTimePlusRefreshLifetime()
	{
		var activeToken = CreateActiveToken(expiresIn: 3600, refreshTokenExpiresIn: 86400);
		
		Assert.Equal(activeToken.CreatedAt.AddSeconds(86400), activeToken.RefreshTokenExpireTime);
	}
	
	#endregion
	
	#region One Time Passwords
	
	[Fact]
	public void ResetPasswordToken_ExpireTime_IsCreationTimePlusLifetime()
	{
		var createdAt = new DateTime(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);
		var token = new ResetPasswordToken("token", TimeSpan.FromMinutes(3), createdAt);
		
		Assert.Equal(createdAt.AddMinutes(3), token.ExpireTime);
	}
	
	[Fact]
	public void ResetPasswordToken_ExpireTime_IsNotSerializedToClients()
	{
		var token = new ResetPasswordToken("token", TimeSpan.FromMinutes(3));
		
		Assert.DoesNotContain("expire_time", System.Text.Json.JsonSerializer.Serialize(token));
		Assert.DoesNotContain("expire_time", Newtonsoft.Json.JsonConvert.SerializeObject(token));
	}
	
	#endregion
	
	#region TTL Indexes
	
	[Fact]
	public void ActiveTokensRepository_DeletesByRetainUntil()
	{
		Assert.Equal("retain_until", GetTTLIndex<ActiveTokensRepository>().Field);
	}
	
	[Fact]
	public void RevokedTokensRepository_DeletesByRetainUntil()
	{
		Assert.Equal("retain_until", GetTTLIndex<RevokedTokensRepository>().Field);
	}
	
	[Fact]
	public void TokenCodeRepository_DeletesByExpireTime()
	{
		Assert.Equal("expire_time", GetTTLIndex<TokenCodeRepository>().Field);
	}
	
	[Fact]
	public void OneTimePasswordRepository_DeletesByTokenExpireTime()
	{
		Assert.Equal("token.expire_time", GetTTLIndex<OneTimePasswordRepository>().Field);
	}
	
	[Fact]
	public void TTLIndexes_DeleteAfterGracePeriod()
	{
		Assert.All(
			[
				GetTTLIndex<ActiveTokensRepository>(),
				GetTTLIndex<RevokedTokensRepository>(),
				GetTTLIndex<TokenCodeRepository>(),
				GetTTLIndex<OneTimePasswordRepository>()
			],
			x => Assert.Equal(TimeSpan.FromMinutes(5), x.ExpireAfter));
	}
	
	#endregion
}
