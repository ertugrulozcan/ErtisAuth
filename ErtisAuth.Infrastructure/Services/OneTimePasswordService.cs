using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Dynamics.Legacy;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Infrastructure.Mapping.Extensions;

namespace ErtisAuth.Infrastructure.Services;

public class OneTimePasswordService : MembershipBoundedCrudService<OneTimePassword, OneTimePasswordDto>, IOneTimePasswordService
{
	#region Constants

	private static readonly char[] Letters = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
	private static readonly char[] Digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
	private static readonly char[] AllChars = Letters.Concat(Digits).ToArray();

	#endregion
	
	#region Services

	private readonly IUserService _userService;

	#endregion
	
    #region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="userService"></param>
	/// <param name="repository"></param>
	public OneTimePasswordService(
		IMembershipService membershipService,
		IUserService userService, 
		IOneTimePasswordRepository repository) : base(membershipService, repository)
	{
		this._userService = userService;
	}

	#endregion
	
	#region Membership Methods
        
	private async Task<Membership> CheckMembershipAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}

		return membership;
	}

	#endregion

	#region Methods
	
	protected override bool ValidateModel(OneTimePassword model, out IEnumerable<string> errors)
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
		
		if (string.IsNullOrEmpty(model.Password))
		{
			errorList.Add($"The {nameof(model.Password)} is required.");
		}
		
		if (model.Token == null)
		{
			errorList.Add($"The {nameof(model.Token)} is required.");
		}
		
		errors = errorList;
		return !errors.Any();
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

	protected override bool IsAlreadyExist(OneTimePassword model, string membershipId, OneTimePassword exclude = default)
	{
		return false;
	}

	protected override async Task<bool> IsAlreadyExistAsync(OneTimePassword model, string membershipId, OneTimePassword exclude = default)
	{
		await Task.CompletedTask;
		return false;
	}

	protected override ErtisAuthException GetAlreadyExistError(OneTimePassword model)
	{
		return null;
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
		
		var dynamicObject = await this._userService.GetAsync(membershipId, userId, cancellationToken: cancellationToken);
		if (dynamicObject == null)
		{
			throw ErtisAuthException.UserNotFound(userId, "userId");
		}
		
		var user = dynamicObject.Deserialize<User>();
		
		var results = await this.QueryAsync(membershipId, QueryBuilder.Equals("user_id", userId).ToString(), 0, 1, cancellationToken: cancellationToken);
		var currentDynamic = results.Items.FirstOrDefault();
		if (currentDynamic != null)
		{
			var otpDynamicObject = new DynamicObject(currentDynamic);
			var currentOtp = otpDynamicObject.Deserialize<OneTimePassword>();
			if (!currentOtp.Token.IsExpired)
			{
				return currentOtp;
			}
		}
		
		var resetPasswordToken = this._userService.GenerateResetPasswordToken(user, membership, true);
		var model = new OneTimePassword
		{
			UserId = user.Id,
			EmailAddress = user.EmailAddress,
			Username = user.Username,
			Password = GenerateCode(membership.OtpSettings.Policy),
			Token = resetPasswordToken,
			MembershipId = membershipId
		};

		return await this.CreateAsync(utilizer, membershipId, model, cancellationToken: cancellationToken);
	}
	
	private static string GenerateCode(OtpPasswordPolicy policy)
	{
		var chars = AllChars.ToArray();
		var onlyDigits = false;
		if (policy.ContainsDigits && !policy.ContainsLetters)
		{
			chars = Digits.ToArray();
			onlyDigits = true;
		}
		else if (policy.ContainsLetters && !policy.ContainsDigits)
		{
			chars = Letters.ToArray();
		}

		var stringBuilder = new StringBuilder();
		var random = new Random(DateTime.Now.Microsecond);
		var beforeIndex = -1;
		for (var i = 0; i < policy.Length; i++)
		{
			var index = random.Next(0, chars.Length);
			if (index == beforeIndex)
			{
				index += random.Next(0, chars.Length);
				index %= chars.Length;
			}

			var character = chars[index];
			if (onlyDigits && i == 0 && character == '0')
			{
				character = Digits[random.Next(1, 9)];
			}
			
			stringBuilder.Append(character);
			beforeIndex = index;
		}
		
		return stringBuilder.ToString().ToUpper();
	}

	public async Task<OneTimePassword> VerifyOtpAsync(
		string username, 
		string password, 
		string membershipId, 
		string host,
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
		
		var query = QueryBuilder.And(
			QueryBuilder.Equals("password", password),
			QueryBuilder.Or(
				QueryBuilder.Equals("username", username),
				QueryBuilder.Equals("email_address", username)
			)
		);
		
		var result = await this.QueryAsync(membershipId, query.ToString(), 0, 1, cancellationToken: cancellationToken);
		var currentDynamic = result.Items.FirstOrDefault();
		if (currentDynamic != null)
		{
			var otpDynamicObject = new DynamicObject(currentDynamic);
			var otp = otpDynamicObject.Deserialize<OneTimePassword>();
			if (otp?.Token != null)
			{
				if (otp.Token.IsExpired)
				{
					throw ErtisAuthException.OtpExpired();
				}

				return otp;
			}
		}
		
		return null;
	}

	public async Task ClearExpiredPasswordsAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		var expiredOtps = new List<OneTimePassword>();
		var results = await this.GetAsync(membershipId, cancellationToken: cancellationToken);
		foreach (var oneTimePassword in results.Items)
		{
			if (oneTimePassword.Token is { IsExpired: true })
			{
				expiredOtps.Add(oneTimePassword);
			}
		}
		
		await this.repository.BulkDeleteAsync(expiredOtps.Select(x => x.ToDto()), cancellationToken: cancellationToken);
	}

	#endregion
}