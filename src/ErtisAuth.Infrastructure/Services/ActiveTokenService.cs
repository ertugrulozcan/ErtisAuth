using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;

namespace ErtisAuth.Infrastructure.Services;

public class ActiveTokenService : MembershipBoundedService<ActiveToken>, IActiveTokenService
{
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="repository"></param>
	public ActiveTokenService(
		IMembershipService membershipService,
		IActiveTokensRepository repository) :
		base(membershipService, repository)
	{
		
	}
	
	#endregion
	
	#region Methods
	
	public async Task<ActiveToken?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
	{
		return await this._repository.FindOneAsync(x => x.AccessToken == accessToken, cancellationToken: cancellationToken);
	}
	
	public async Task<ActiveToken?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
	{
		return await this._repository.FindOneAsync(x => x.RefreshToken == refreshToken, cancellationToken: cancellationToken);
	}
	
	public async Task<ActiveToken> CreateAsync(
		BearerToken token, 
		User user, 
		string membershipId, 
		string? ipAddress = null, 
		string? userAgent = null,
		CancellationToken cancellationToken = default)
	{
		var clientInfo = this.GenerateClientInfo(ipAddress, userAgent);
		return await this._repository.InsertAsync(new ActiveToken
		{
			AccessToken = token.AccessToken,
			RefreshToken = token.RefreshToken,
			ExpiresIn = token.ExpiresInTimeStamp,
			RefreshTokenExpiresIn = token.RefreshTokenExpiresInTimeStamp,
			TokenType = token.TokenType.ToString(),
			CreatedAt = token.CreatedAt,
			UserId = user.Id,
			UserName = user.Username,
			EmailAddress = user.EmailAddress,
			FirstName = user.FirstName,
			LastName = user.LastName,
			MembershipId = membershipId,
			ClientInfo = clientInfo
		}, cancellationToken: cancellationToken);
	}
	
	private ClientInfo GenerateClientInfo(string? ipAddress, string? userAgent)
	{
		var clientInfo = new ClientInfo
		{
			IPAddress = ipAddress,
			UserAgent = userAgent
		};
		
		return clientInfo;
	}
	
	public async Task<IEnumerable<ActiveToken>> GetActiveTokensByUser(string userId, string membershipId, CancellationToken cancellationToken = default)
	{
		var expiredActiveTokensResult = await this._repository.FindAsync(x => x.UserId == userId && x.MembershipId == membershipId, sorting: null, cancellationToken: cancellationToken);
		return expiredActiveTokensResult.Items;
	}
	
	public async Task BulkDeleteAsync(IEnumerable<ActiveToken> activeTokens, CancellationToken cancellationToken = default)
	{
		await this._repository.BulkDeleteAsync(activeTokens, cancellationToken: cancellationToken);
	}
	
	#endregion
}