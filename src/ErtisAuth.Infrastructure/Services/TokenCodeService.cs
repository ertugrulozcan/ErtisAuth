using System.Security.Cryptography;
using System.Text;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Helpers;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace ErtisAuth.Infrastructure.Services;

/// <summary>
/// Device login (e.g. a smart TV), like the OAuth 2.0 device authorization grant (RFC 8628): the device shows the user
/// code and polls with the device code, which only it knows; a signed in user approves or denies the user code.
/// </summary>
public class TokenCodeService : MembershipBoundedService<TokenCode>, ITokenCodeService
{
	#region Constants
	
	/// <summary>
	/// The minimum number of seconds between two polls of a device.
	/// </summary>
	public const int PollInterval = 5;
	
	private const int DeviceCodeByteCount = 32;
	
	private const int MaxGenerationAttempts = 10;
	
	#endregion
	
	#region Services
	
	private readonly ITokenCodePolicyService _tokenCodePolicyService;
	private readonly ITokenService _tokenService;
	private readonly IUserService _userService;
	private readonly IEventService _eventService;
	private readonly ITokenCodeRepository _tokenCodeRepository;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="tokenCodePolicyService"></param>
	/// <param name="tokenService"></param>
	/// <param name="userService"></param>
	/// <param name="eventService"></param>
	/// <param name="repository"></param>
	public TokenCodeService(
		IMembershipService membershipService,
		ITokenCodePolicyService tokenCodePolicyService,
		ITokenService tokenService,
		IUserService userService,
		IEventService eventService,
		ITokenCodeRepository repository) :
		base(membershipService, repository)
	{
		this._tokenCodePolicyService = tokenCodePolicyService;
		this._tokenService = tokenService;
		this._userService = userService;
		this._eventService = eventService;
		this._tokenCodeRepository = repository;
	}
	
	#endregion
	
	#region Create Methods
	
	public async Task<TokenCodeWithDeviceCode> CreateAsync(string membershipId, ClientInfo? clientInfo = null, CancellationToken cancellationToken = default)
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
		
		var deviceCode = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(DeviceCodeByteCount));
		var now = DateTime.UtcNow;
		
		// The unique index of the user codes rejects a code which is in use; a new one is generated then
		for (var i = 0; i < MaxGenerationAttempts; i++)
		{
			try
			{
				var tokenCode = await this._repository.InsertAsync(new TokenCode
				{
					UserCode = RandomCodeGenerator.Generate(policy.Length, policy.ContainsLetters, policy.ContainsDigits),
					DeviceCodeHash = HashDeviceCode(deviceCode),
					Status = TokenCodeStatus.Pending,
					ExpiresIn = policy.ExpiresIn,
					Interval = PollInterval,
					CreatedAt = now,
					ExpireTime = now.AddSeconds(policy.ExpiresIn),
					ClientInfo = clientInfo,
					MembershipId = membershipId
				}, cancellationToken: cancellationToken);
				
				return new TokenCodeWithDeviceCode
				{
					Id = tokenCode.Id,
					UserCode = tokenCode.UserCode,
					Status = tokenCode.Status,
					ExpiresIn = tokenCode.ExpiresIn,
					Interval = tokenCode.Interval,
					CreatedAt = tokenCode.CreatedAt,
					ExpireTime = tokenCode.ExpireTime,
					ClientInfo = tokenCode.ClientInfo,
					MembershipId = tokenCode.MembershipId,
					DeviceCode = deviceCode
				};
			}
			catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
			{
				// The user code is in use, try another one
			}
		}
		
		throw ErtisAuthException.TokenCodeCouldNotBeGenerated();
	}
	
	/// <summary>
	/// The device code is a 256-bit random value, so a plain hash is enough to keep a leaked database from revealing it.
	/// </summary>
	private static string HashDeviceCode(string deviceCode)
	{
		return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(deviceCode)));
	}
	
	#endregion
	
	#region Approval Methods
	
	public async Task<TokenCode> GetByUserCodeAsync(string userCode, string membershipId, CancellationToken cancellationToken = default)
	{
		var tokenCode = await this.FindByUserCodeAsync(userCode, membershipId, cancellationToken: cancellationToken);
		if (tokenCode == null || tokenCode.IsExpired)
		{
			throw ErtisAuthException.TokenCodeNotFound();
		}
		
		return tokenCode;
	}
	
	public async Task<TokenCode> ApproveAsync(string userCode, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		return await this.DecideAsync(userCode, membershipId, utilizer, TokenCodeStatus.Approved, ErtisAuthEventType.TokenCodeApproved, cancellationToken: cancellationToken);
	}
	
	public async Task<TokenCode> DenyAsync(string userCode, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		return await this.DecideAsync(userCode, membershipId, utilizer, TokenCodeStatus.Denied, ErtisAuthEventType.TokenCodeDenied, cancellationToken: cancellationToken);
	}
	
	private async Task<TokenCode> DecideAsync(string userCode, string membershipId, Utilizer utilizer, string status, ErtisAuthEventType eventType, CancellationToken cancellationToken = default)
	{
		var user = await this._userService.GetUserAsync(utilizer.Id!, membershipId, cancellationToken: cancellationToken);
		if (user == null)
		{
			throw ErtisAuthException.UserNotFound(utilizer.Id ?? string.Empty, "id");
		}
		
		var normalizedUserCode = TokenCode.NormalizeUserCode(userCode);
		// A scoped token approves for its scopes only: the device can't get more than the approving session has
		var scopes = status == TokenCodeStatus.Approved && utilizer.Scopes is { Length: > 0 } ? utilizer.Scopes : null;
		var decided = await this._tokenCodeRepository.TryDecideAsync(normalizedUserCode, membershipId, status, user.Id, scopes, DateTime.UtcNow, cancellationToken: cancellationToken);
		if (decided == null)
		{
			// Why the code could not be decided: re-deciding would log the waiting device in as another user
			var tokenCode = await this.FindByUserCodeAsync(normalizedUserCode, membershipId, cancellationToken: cancellationToken);
			if (tokenCode == null)
			{
				throw ErtisAuthException.TokenCodeNotFound();
			}
			
			if (tokenCode.IsExpired)
			{
				throw ErtisAuthException.TokenCodeExpired();
			}
			
			throw ErtisAuthException.TokenCodeAlreadyAuthorized();
		}
		
		// The device code never leaves the creation response
		var codeInfo = new
		{
			user_code = decided.UserCode,
			client_info = decided.ClientInfo,
			created_at = decided.CreatedAt
		};
		
		await this._eventService.FireEventAsync(eventType, utilizer, membershipId, new { user, code = codeInfo }, cancellationToken: cancellationToken);
		return decided;
	}
	
	private async Task<TokenCode?> FindByUserCodeAsync(string userCode, string membershipId, CancellationToken cancellationToken = default)
	{
		var normalizedUserCode = TokenCode.NormalizeUserCode(userCode);
		return await this._repository.FindOneAsync(x => x.UserCode == normalizedUserCode && x.MembershipId == membershipId, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Token Methods
	
	public async Task<BearerToken> GenerateTokenAsync(string deviceCode, string membershipId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(deviceCode))
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		var deviceCodeHash = HashDeviceCode(deviceCode);
		var tokenCode = await this._repository.FindOneAsync(x => x.DeviceCodeHash == deviceCodeHash && x.MembershipId == membershipId, cancellationToken: cancellationToken);
		if (tokenCode == null)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		if (tokenCode.IsExpired)
		{
			throw ErtisAuthException.TokenCodeExpired();
		}
		
		if (!await this._tokenCodeRepository.TryRegisterPollAsync(tokenCode.Id, tokenCode.Interval, DateTime.UtcNow, cancellationToken: cancellationToken))
		{
			throw ErtisAuthException.TokenCodeSlowDown(tokenCode.Interval);
		}
		
		switch (tokenCode.Status)
		{
			case TokenCodeStatus.Pending:
				throw ErtisAuthException.UnauthorizedTokenCode();
			case TokenCodeStatus.Denied:
				throw ErtisAuthException.TokenCodeDenied();
		}
		
		// Single use: only one request deletes the approved code and gets the token
		var consumed = await this._tokenCodeRepository.TryConsumeAsync(tokenCode.Id, cancellationToken: cancellationToken);
		if (consumed?.UserId == null)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		var user = await this._userService.GetUserAsync(consumed.UserId, membershipId, cancellationToken: cancellationToken);
		if (user == null)
		{
			throw ErtisAuthException.UserNotFound(consumed.UserId, "id");
		}
		
		if (!user.IsActive)
		{
			throw ErtisAuthException.UserInactive(user.Id);
		}
		
		// Generated now (not at the approval): its lifetime starts when the device gets it, and it is never stored here
		var ipAddress = consumed.ClientInfo?.IPAddress;
		var userAgent = consumed.ClientInfo?.UserAgent;
		return consumed.Scopes is { Length: > 0 }
			? await this._tokenService.GenerateScopedTokenAsync(user, consumed.Scopes, membershipId, ipAddress, userAgent, cancellationToken: cancellationToken)
			: await this._tokenService.GenerateTokenAsync(user, membershipId, ipAddress, userAgent, cancellationToken: cancellationToken);
	}
	
	#endregion
}
