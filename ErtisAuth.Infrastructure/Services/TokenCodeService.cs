using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Infrastructure.Mapping.Extensions;

namespace ErtisAuth.Infrastructure.Services;

public class TokenCodeService : MembershipBoundedService<TokenCode, TokenCodeDto>, ITokenCodeService
{
	#region Constants

	private static readonly char[] Letters = new [] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
	private static readonly char[] Digits = new [] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
	private static readonly char[] AllChars = Letters.Concat(Digits).ToArray();

	#endregion
	
	#region Services

	private readonly ITokenCodePolicyService _tokenCodePolicyService;

	#endregion
	
    #region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="tokenCodePolicyService"></param>
	/// <param name="repository"></param>
	public TokenCodeService(
		IMembershipService membershipService,
		ITokenCodePolicyService tokenCodePolicyService, 
		ITokenCodeRepository repository) : 
		base(membershipService, repository)
	{
		this._tokenCodePolicyService = tokenCodePolicyService;
	}

	#endregion

	#region Methods
	
	public async Task<TokenCode> CreateAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
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
		var current = await this.repository.FindAsync(x => x.Code == code, 0, 1, false, null, null, cancellationToken: cancellationToken);
		while (current.Items.Any())
		{
			code = GenerateCode(policy);
			current = await this.repository.FindAsync(x => x.Code == code, 0, 1, false, null, null, cancellationToken: cancellationToken);
		}
		
		var insertedDto = await this.repository.InsertAsync(new TokenCodeDto
		{
			Code = code,
			ExpiresIn = policy.ExpiresIn,
			CreatedAt = DateTime.Now,
			MembershipId = membershipId
		}, cancellationToken: cancellationToken);

		return insertedDto.ToModel();
	}

	private static string GenerateCode(TokenCodePolicy policy)
	{
		var chars = AllChars.ToArray();
		if (policy.ContainsDigits && !policy.ContainsLetters)
		{
			chars = Digits.ToArray();
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
			
			stringBuilder.Append(chars[index]);
			beforeIndex = index;
		}
		
		return stringBuilder.ToString();
	}
	
	public async ValueTask ClearExpiredTokenCodes(string membershipId, CancellationToken cancellationToken = default)
	{
		try
		{
			var expiredTokenCodesResult = await this.repository.FindAsync(x => x.MembershipId == membershipId && x.ExpireTime < DateTime.Now, sorting: null, cancellationToken: cancellationToken);
			var expiredTokenCodes = expiredTokenCodesResult.Items.ToArray();
			if (expiredTokenCodes.Any())
			{
				var isDeleted = await this.repository.BulkDeleteAsync(expiredTokenCodes, cancellationToken: cancellationToken);
				if (isDeleted)
				{
					Console.WriteLine($"{expiredTokenCodes.Length} expired token code cleared");
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