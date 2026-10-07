using Ertis.Core.Models;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

// ReSharper disable UnusedType.Global
namespace ErtisAuth.Sdk.Services;

public class PasswordService : MembershipBoundedService, IPasswordService
{
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="ertisAuthOptions"></param>
	/// <param name="restHandler"></param>
	public PasswordService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
	{
		
	}
	
	#endregion
	
	#region Methods
	
	public async Task<IResponseResult> ChangePasswordAsync(string userId, string newPassword, TokenBase token, CancellationToken cancellationToken = default)
	{
		return await this.ExecuteRequestAsync(
			HttpMethod.Put, 
			$"{this.BaseUrl}/memberships/{this.MembershipId}/users/{userId}/change-password", 
			null, 
			HeaderCollection.Add("Authorization", token.ToString()),
			new JsonRequestBody(new { password = newPassword }),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult> ResetPasswordAsync(string emailAddress, string host, TokenBase token, CancellationToken cancellationToken = default)
	{
		return await this.ExecuteRequestAsync(
			HttpMethod.Post, 
			$"{this.BaseUrl}/memberships/{this.MembershipId}/users/reset-password", 
			null, 
			HeaderCollection.Add("Authorization", token.ToString()).Add("X-Host", host),
			new JsonRequestBody(new
			{
				email_address = emailAddress
			}),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult> SetPasswordAsync(string email, string password, string resetToken, TokenBase token, CancellationToken cancellationToken = default)
	{
		return await this.ExecuteRequestAsync(
			HttpMethod.Post, 
			$"{this.BaseUrl}/memberships/{this.MembershipId}/users/set-password", 
			null, 
			HeaderCollection.Add("Authorization", token.ToString()),
			new JsonRequestBody(new { email_address = email, reset_token = resetToken, password }),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	#endregion
}