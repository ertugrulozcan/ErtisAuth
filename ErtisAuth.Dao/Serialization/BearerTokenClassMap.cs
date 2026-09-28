using MongoDB.Bson.Serialization;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Dao.Serialization;

/// <summary>
/// BSON mapping of BearerToken (stored inside TokenCode). BearerToken has no parameterless constructor and its
/// expirations are serialized as computed seconds, so it is restored through its constructor.
/// </summary>
public static class BearerTokenClassMap
{
	#region Methods
	
	public static void Register()
	{
		if (BsonClassMap.IsClassMapRegistered(typeof(BearerToken)))
		{
			return;
		}
		
		BsonClassMap.RegisterClassMap<BearerToken>(classMap =>
		{
			classMap.AutoMap();
			
			// token_type is already mapped by the abstract TokenBase.TokenType
			classMap.UnmapProperty(nameof(BearerToken.TokenType));
			
			classMap
				.MapCreator((Func<string, int, string?, int, DateTime, BearerToken>) ((accessToken, expiresIn, refreshToken, refreshExpiresIn, createdAt) =>
					new BearerToken(accessToken, TimeSpan.FromSeconds(expiresIn), refreshToken, TimeSpan.FromSeconds(refreshExpiresIn), createdAt)))
				.SetArguments([
					nameof(TokenBase.AccessToken),
					nameof(TokenBase.ExpiresInTimeStamp),
					nameof(BearerToken.RefreshToken),
					nameof(BearerToken.RefreshTokenExpiresInTimeStamp),
					nameof(TokenBase.CreatedAt)
				]);
			
			// Tokens stored before created_at was persisted (master, BearerTokenDto) are treated as expired
			classMap
				.MapCreator((Func<string, int, string?, int, BearerToken>) ((accessToken, expiresIn, refreshToken, refreshExpiresIn) =>
					new BearerToken(accessToken, TimeSpan.FromSeconds(expiresIn), refreshToken, TimeSpan.FromSeconds(refreshExpiresIn), DateTime.MinValue)))
				.SetArguments([
					nameof(TokenBase.AccessToken),
					nameof(TokenBase.ExpiresInTimeStamp),
					nameof(BearerToken.RefreshToken),
					nameof(BearerToken.RefreshTokenExpiresInTimeStamp)
				]);
		});
	}
	
	#endregion
}