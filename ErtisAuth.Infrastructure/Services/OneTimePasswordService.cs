using System.Security.Cryptography;
using System.Text;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class OneTimePasswordService : MembershipBoundedCrudService<OneTimePassword>, IOneTimePasswordService
{
	#region Services
	
	private readonly IUserService _userService;
	private readonly IPasswordResetService _passwordResetService;
	private readonly IOneTimePasswordRepository _oneTimePasswordRepository;
	private readonly ILogger<OneTimePasswordService> _logger;
	
	#endregion
	
    #region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="userService"></param>
	/// <param name="passwordResetService"></param>
	/// <param name="repository"></param>
	/// <param name="logger"></param>
	public OneTimePasswordService(
		IMembershipService membershipService,
		IUserService userService, 
		IPasswordResetService passwordResetService,
		IOneTimePasswordRepository repository,
		ILogger<OneTimePasswordService> logger) : base(membershipService, repository)
	{
		this._userService = userService;
		this._passwordResetService = passwordResetService;
		this._oneTimePasswordRepository = repository;
		this._logger = logger;
	}
	
	#endregion
	
	#region Membership Methods
        
	private async Task<Membership> CheckMembershipAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		return membership;
	}
	
	#endregion
	
	#region Methods
	
	protected override Task<IEnumerable<string>> ValidateModelAsync(OneTimePassword model, CancellationToken cancellationToken = default)
	{
		var errorList = new List<string>();
		
		if (string.IsNullOrEmpty(model.UserId))
		{
			errorList.Add($"The {nameof(model.UserId)} is required.");
		}
		
		if (string.IsNullOrEmpty(model.EmailAddress))
		{
			errorList.Add($"The {nameof(model.EmailAddress)} is required.");
		}
		
		if (string.IsNullOrEmpty(model.Username))
		{
			errorList.Add($"The {nameof(model.Username)} is required.");
		}
		
		if (string.IsNullOrEmpty(model.MembershipId))
		{
			errorList.Add($"The {nameof(model.MembershipId)} is required.");
		}
		
		if (string.IsNullOrEmpty(model.PasswordHash))
		{
			errorList.Add($"The {nameof(model.PasswordHash)} is required.");
		}
		
		if (model.Token == null)
		{
			errorList.Add($"The {nameof(model.Token)} is required.");
		}
		
		return Task.FromResult<IEnumerable<string>>(errorList);
	}
	
	protected override void Overwrite(OneTimePassword destination, OneTimePassword source)
	{
		destination.Id = source.Id;
		destination.MembershipId = source.MembershipId;
		
		if (this.IsIdentical(destination, source))
		{
			throw ErtisAuthException.IdenticalDocument();
		}
	}
	
	protected override async Task<bool> IsAlreadyExistAsync(OneTimePassword model, string membershipId, OneTimePassword? exclude = null, CancellationToken cancellationToken = default)
	{
		return await Task.FromResult(false);
	}
	
	protected override ErtisAuthException GetAlreadyExistError(OneTimePassword model)
	{
		return ErtisAuthException.OneTimePasswordAlreadyExists();
	}
	
	protected override ErtisAuthException GetNotFoundError(string id)
	{
		return ErtisAuthException.OneTimePasswordNotFound(id);
	}
	
	public async Task<OneTimePassword> GenerateAsync(
		Utilizer utilizer, 
		string membershipId, 
		string userId,
		CancellationToken cancellationToken = default)
	{
		var membership = await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
		if (membership.OtpSettings?.Policy == null)
		{
			throw ErtisAuthException.OtpNotConfiguredYet();
		}
		
		if (string.IsNullOrEmpty(membership.OtpSettings?.Host))
		{
			throw ErtisAuthException.OtpHostNotConfiguredYet();
		}
		
		var user = await this._userService.GetUserAsync(userId, membershipId, cancellationToken: cancellationToken);
		if (user == null)
		{
			throw ErtisAuthException.UserNotFound(userId, "userId");
		}
		
		// Only the hash of a code is stored, so an active code can't be returned again: a new one replaces it
		var previousOtps = await this._repository.FindAsync(x => x.MembershipId == membershipId && x.UserId == user.Id, sorting: null, cancellationToken: cancellationToken);
		foreach (var previousOtp in previousOtps.Items)
		{
			await this._repository.DeleteAsync(previousOtp.Id, cancellationToken: cancellationToken);
		}
		
		var policy = membership.OtpSettings.Policy;
		var code = RandomCodeGenerator.Generate(policy.Length, policy.ContainsLetters, policy.ContainsDigits);
		var resetPasswordToken = await this._passwordResetService.GenerateResetPasswordTokenAsync(user, membership, true, ResetPasswordToken.ResetPasswordTokenPurpose.OneTimePassword, cancellationToken: cancellationToken);
		var model = new OneTimePassword
		{
			UserId = user.Id,
			EmailAddress = user.EmailAddress,
			Username = user.Username,
			PasswordHash = HashCode(membership, user.Id, code),
			Token = resetPasswordToken,
			MembershipId = membershipId
		};
		
		var created = await this.CreateAsync(model, membershipId, utilizer, cancellationToken: cancellationToken);
		created.Password = code;
		return created;
	}
	
	/// <summary>
	/// HMAC-SHA256 keyed with the membership secret: a leaked database alone doesn't allow brute-forcing the short codes offline.
	/// Codes are compared case-insensitively.
	/// </summary>
	private static string HashCode(Membership membership, string userId, string code)
	{
		var key = Encoding.UTF8.GetBytes(membership.SecretKey);
		var message = Encoding.UTF8.GetBytes($"otp:{userId}:{code.ToUpperInvariant()}");
		return Convert.ToHexStringLower(HMACSHA256.HashData(key, message));
	}
	
	private static bool IsMatch(Membership membership, OneTimePassword otp, string code)
	{
		if (string.IsNullOrEmpty(otp.UserId) || string.IsNullOrEmpty(otp.PasswordHash))
		{
			return false;
		}
		
		var expected = Encoding.ASCII.GetBytes(otp.PasswordHash);
		var actual = Encoding.ASCII.GetBytes(HashCode(membership, otp.UserId, code));
		return CryptographicOperations.FixedTimeEquals(expected, actual);
	}
	
	public async Task<OneTimePassword?> VerifyOtpAsync(
		string username, 
		string password, 
		string membershipId, 
		string? host,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(host))
		{
			throw ErtisAuthException.OtpHostRequired();
		}
		
		var membership = await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
		if (membership.OtpSettings?.Host != host)
		{
			throw ErtisAuthException.OtpHostMismatch();
		}
		
		var otp = await this._repository.FindOneAsync(x => x.MembershipId == membershipId && (x.Username == username || x.EmailAddress == username), cancellationToken: cancellationToken);
		if (otp?.Token == null)
		{
			return null;
		}
		
		// The attempt is reserved before the code is compared, so parallel requests can't evaluate more guesses than allowed
		var maxAttempts = membership.OtpSettings?.Policy?.MaxAttempts ?? OtpPasswordPolicy.DefaultMaxAttempts;
		var reserved = await this._oneTimePasswordRepository.TryReserveAttemptAsync(otp.Id, maxAttempts, cancellationToken: cancellationToken);
		if (reserved == null)
		{
			// The attempts are used up (or the one-time password is gone)
			await this._repository.DeleteAsync(otp.Id, cancellationToken: cancellationToken);
			return null;
		}
		
		if (!IsMatch(membership, otp, password))
		{
			if (reserved.FailedAttempts >= maxAttempts)
			{
				await this._repository.DeleteAsync(otp.Id, cancellationToken: cancellationToken);
			}
			
			return null;
		}
		
		// Not a failed attempt: give the reservation back
		await this._oneTimePasswordRepository.ReleaseAttemptAsync(otp.Id, cancellationToken: cancellationToken);
		
		if (otp.Token.IsExpired)
		{
			throw ErtisAuthException.OtpExpired();
		}
		
		// The one-time password stays until set-password consumes its reset token (RevokeResetPasswordTokenAsync)
		return otp;
	}
	
	public async Task RevokeResetPasswordTokenAsync(Utilizer utilizer, string membershipId, string resetToken, CancellationToken cancellationToken = default)
	{
		try
		{
			await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
			var otp = await this._repository.FindOneAsync(x => x.MembershipId == membershipId && x.Token != null && x.Token.Token == resetToken, cancellationToken: cancellationToken);
			if (otp != null)
			{
				await this.DeleteAsync(otp.Id, membershipId, utilizer, cancellationToken: cancellationToken);
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "OneTimePasswordService.RevokeResetPasswordTokenAsync occured an error");
		}
	}
	
	#endregion
}