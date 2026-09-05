using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Infrastructure.Mapping.Extensions;

namespace ErtisAuth.Infrastructure.Services;

public class ActiveTokenService : MembershipBoundedService<ActiveToken, ActiveTokenDto>, IActiveTokenService
{
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="repository"></param>
	public ActiveTokenService(IMembershipService membershipService, IActiveTokensRepository repository) : base(membershipService, repository)
	{ }
	
	#endregion
	
	#region Methods
	
	public async Task<ActiveToken?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
	{
		var dto = await this.repository.FindOneAsync(x => x.AccessToken == accessToken, cancellationToken: cancellationToken);
		return dto?.ToModel();
	}
	
	public async Task<ActiveToken?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
	{
		var dto = await this.repository.FindOneAsync(x => x.RefreshToken == refreshToken, cancellationToken: cancellationToken);
		return dto?.ToModel();
	}
	
	public async Task<ActiveToken> CreateAsync(
		BearerToken token, 
		User user, 
		string membershipId, 
		string? ipAddress = null, 
		string? userAgent = null,
		CancellationToken cancellationToken = default)
	{
		var clientInfo = this.GetClientInfo(ipAddress, userAgent);
		var insertedDto = await this.repository.InsertAsync(new ActiveTokenDto
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
			ClientInfo = clientInfo.ToDto()
		}, cancellationToken: cancellationToken);
		
		return insertedDto.ToModel();
	}
	
	private ClientInfo GetClientInfo(string? ipAddress, string? userAgent)
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
		var expiredActiveTokensResult = await this.repository.FindAsync(x => x.UserId == userId && x.MembershipId == membershipId, sorting: null, cancellationToken: cancellationToken);
		return expiredActiveTokensResult.Items.Select(x => x.ToModel());
	}
	
	public async Task BulkDeleteAsync(IEnumerable<ActiveToken> activeTokens, CancellationToken cancellationToken = default)
	{
		await this.repository.BulkDeleteAsync(activeTokens.Select(x => x.ToDto()), cancellationToken: cancellationToken);
	}
	
	public async ValueTask ClearExpiredActiveTokens(string membershipId, CancellationToken cancellationToken = default)
	{
		try
		{
			var expiredActiveTokensResult = await this.repository.FindAsync(x => x.MembershipId == membershipId && x.ExpireTime < DateTime.Now, sorting: null, cancellationToken: cancellationToken);
			var expiredActiveTokens = expiredActiveTokensResult.Items.ToArray();
			if (expiredActiveTokens.Any())
			{
				var isDeleted = await this.repository.BulkDeleteAsync(expiredActiveTokens, cancellationToken: cancellationToken);
				if (isDeleted)
				{
					Console.WriteLine($"{expiredActiveTokens.Length} expired active token cleared");
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}
	}
	
	#endregion
}