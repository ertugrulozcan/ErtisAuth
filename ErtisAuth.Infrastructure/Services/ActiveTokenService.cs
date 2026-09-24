using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class ActiveTokenService : MembershipBoundedService<ActiveToken>, IActiveTokenService
{
	#region Services
	
	private readonly ILogger<ActiveTokenService> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="repository"></param>
	/// <param name="logger"></param>
	public ActiveTokenService(
		IMembershipService membershipService,
		IActiveTokensRepository repository,
		ILogger<ActiveTokenService> logger) :
		base(membershipService, repository)
	{
		this._logger = logger;
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
	
	public async ValueTask ClearExpiredActiveTokens(string membershipId, CancellationToken cancellationToken = default)
	{
		try
		{
			var expiredActiveTokensResult = await this._repository.FindAsync(x => x.MembershipId == membershipId && x.ExpireTime < DateTime.Now, sorting: null, cancellationToken: cancellationToken);
			var expiredActiveTokens = expiredActiveTokensResult.Items.ToArray();
			if (expiredActiveTokens.Any())
			{
				var isDeleted = await this._repository.BulkDeleteAsync(expiredActiveTokens, cancellationToken: cancellationToken);
				if (isDeleted)
				{
					this._logger.LogInformation("{Count} expired active token cleared", expiredActiveTokens.Length);
				}
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "ActiveTokenService.ClearExpiredActiveTokens occured an error");
		}
	}
	
	#endregion
}