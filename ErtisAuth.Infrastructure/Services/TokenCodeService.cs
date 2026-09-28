using Ertis.Data.Models;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class TokenCodeService : MembershipBoundedService<TokenCode>, ITokenCodeService
{
	#region Services
	
	private readonly ITokenCodePolicyService _tokenCodePolicyService;
	private readonly ITokenService _tokenService;
	private readonly IUserService _userService;
	private readonly ILogger<TokenCodeService> _logger;
	
	#endregion
	
    #region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="tokenCodePolicyService"></param>
	/// <param name="tokenService"></param>
	/// <param name="userService"></param>
	/// <param name="repository"></param>
	/// <param name="logger"></param>
	public TokenCodeService(
		IMembershipService membershipService,
		ITokenCodePolicyService tokenCodePolicyService,
		ITokenService tokenService,
		IUserService userService,
		ITokenCodeRepository repository,
		ILogger<TokenCodeService> logger) : 
		base(membershipService, repository)
	{
		this._tokenCodePolicyService = tokenCodePolicyService;
		this._tokenService = tokenService;
		this._userService = userService;
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	private async Task<TokenCode?> GetTokenCode(
		string code, 
		string membershipId,
		CancellationToken cancellationToken = default)
	{
		var results = await this._repository.FindAsync(x => x.Code == code && x.MembershipId == membershipId, 0, 1, false, null, null, null, cancellationToken: cancellationToken);
		return results.Items.FirstOrDefault();
	}
	
	public async Task<TokenCode> CreateAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		if (string.IsNullOrEmpty(membership.CodePolicy))
		{
			throw ErtisAuthException.TokenCodePolicyNotFound();
		}
		
		var policy = await this._tokenCodePolicyService.GetBySlugAsync(membership.CodePolicy, membershipId, cancellationToken: cancellationToken);
		if (policy == null)
		{
			throw ErtisAuthException.TokenCodePolicyNotFound(membership.CodePolicy);
		}
		
		var code = GenerateCode(policy);
		var current = await this._repository.FindAsync(x => x.Code == code && x.MembershipId == membershipId, 0, 1, false, null, null, null, cancellationToken: cancellationToken);
		while (current.Items.Any())
		{
			code = GenerateCode(policy);
			current = await this._repository.FindAsync(x => x.Code == code && x.MembershipId == membershipId, 0, 1, false, null, null, null, cancellationToken: cancellationToken);
		}
		
		return await this._repository.InsertAsync(new TokenCode
		{
			Code = code,
			ExpiresIn = policy.ExpiresIn,
			CreatedAt = DateTime.UtcNow,
			MembershipId = membershipId
		}, cancellationToken: cancellationToken);
	}
	
	private static string GenerateCode(TokenCodePolicy policy)
	{
		return RandomCodeGenerator.Generate(policy.Length, policy.ContainsLetters, policy.ContainsDigits);
	}
	
	public async Task<TokenCode> AuthorizeCodeAsync(string code, Utilizer utilizer, string membershipId, CancellationToken cancellationToken = default)
	{
		var tokenCode = await this.GetTokenCode(code, membershipId, cancellationToken: cancellationToken);
		if (tokenCode == null)
		{
			throw ErtisAuthException.InvalidTokenCode();
		}
		
		if (tokenCode.ExpireTime < DateTime.UtcNow)
		{
			throw ErtisAuthException.TokenCodeExpired();
		}
		
		// Re-approving would log the waiting device in as another user
		if (tokenCode.Token != null)
		{
			throw ErtisAuthException.TokenCodeAlreadyAuthorized();
		}
		
		var user = await this._userService.GetUserAsync(membershipId, utilizer.Id!, cancellationToken: cancellationToken);
		if (user == null)
		{
			throw ErtisAuthException.UserNotFound(utilizer.Id ?? string.Empty, "id");
		}
		
		var token = await this._tokenService.GenerateTokenAsync(user, membershipId, cancellationToken: cancellationToken);
		tokenCode.AssignToken(token, user.Id);
		
		return await this._repository.UpdateAsync(tokenCode, tokenCode.Id, new UpdateOptions
		{
			TriggerBeforeActionBinder = false,
			TriggerAfterActionBinder = false
		}, cancellationToken: cancellationToken);
	}
	
	public async Task<BearerToken> GenerateTokenAsync(string code, string membershipId, CancellationToken cancellationToken = default)
	{
		var tokenCode = await this.GetTokenCode(code, membershipId, cancellationToken: cancellationToken);
		if (tokenCode == null)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		if (tokenCode.ExpireTime < DateTime.UtcNow)
		{
			throw ErtisAuthException.TokenCodeExpired();
		}
		
		if (tokenCode.Token == null)
		{
			throw ErtisAuthException.UnauthorizedTokenCode();
		}
		
		if (tokenCode.Token.IsExpired)
		{
			throw ErtisAuthException.TokenWasExpired();
		}
		
		// Single use: the token is handed out once, to the polling device
		await this._repository.DeleteAsync(tokenCode.Id, cancellationToken: cancellationToken);
		return tokenCode.Token;
	}
	
	#endregion
}