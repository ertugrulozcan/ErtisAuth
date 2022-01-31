using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ErtisAuth.Hub.ViewModels.Auth;
using ErtisAuth.Sdk.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Hub.Controllers.API
{
    [Route("api/forgot-password")]
    public class ForgotPasswordController : ControllerBase
    {
	    #region Methods
        
        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest forgotPasswordRequest)
        {
	        try
	        {
		        if (string.IsNullOrEmpty(forgotPasswordRequest.EmailAddress))
        		{
        			return this.BadRequest("Email is required");
        		}
		        
		        if (string.IsNullOrEmpty(forgotPasswordRequest.ServerUrl))
		        {
			        return this.BadRequest("ServerUrl is required");
		        }
		        
		        if (string.IsNullOrEmpty(forgotPasswordRequest.Host))
		        {
			        return this.BadRequest("Host is required");
		        }
	            
	            if (string.IsNullOrEmpty(forgotPasswordRequest.EncryptedSecretKey))
	            {
		            return this.BadRequest("SecretKey is required");
	            }
	            
	            if (string.IsNullOrEmpty(forgotPasswordRequest.MembershipId))
	            {
		            return this.BadRequest("MembershipId is required");
	            }
	            
	            var serviceScopeFactory = this.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
	            using (var scope = serviceScopeFactory.CreateScope())
	            {
		            var scopedErtisAuthOptions = scope.ServiceProvider.GetRequiredService<IErtisAuthOptions>() as ScopedErtisAuthOptions;
		            scopedErtisAuthOptions?.SetTemporary($"{forgotPasswordRequest.ServerUrl}", forgotPasswordRequest.MembershipId);
		            var scopedPasswordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
		            
		            var secretKey = Identity.Cryptography.StringCipher.Decrypt(forgotPasswordRequest.EncryptedSecretKey, forgotPasswordRequest.MembershipId);
		            var basicToken = new BasicToken($"ertisauth_server:{secretKey}");
		            var resetPasswordResponse = await scopedPasswordService.ResetPasswordAsync(forgotPasswordRequest.EmailAddress, forgotPasswordRequest.ServerUrl, forgotPasswordRequest.ClearHost, basicToken);
		            if (resetPasswordResponse.IsSuccess)
		            {
			            var encryptedResetPasswordToken = Identity.Cryptography.StringCipher.Encrypt(resetPasswordResponse.Data.Token, forgotPasswordRequest.MembershipId);
			            var payloadDictionary = new Dictionary<string, string>
			            {
				            { "emailAddress", forgotPasswordRequest.EmailAddress },
				            { "encryptedSecretKey", forgotPasswordRequest.EncryptedSecretKey },
				            { "serverUrl", forgotPasswordRequest.ServerUrl },
				            { "membershipId", forgotPasswordRequest.MembershipId },
				            { "encryptedResetPasswordToken", encryptedResetPasswordToken },
			            };

			            var encodedPayload = Identity.Cryptography.StringCipher.Encrypt(string.Join('&', payloadDictionary.Select(x => $"{x.Key}={x.Value}")), forgotPasswordRequest.MembershipId);
			            var resetPasswordToken = $"{forgotPasswordRequest.MembershipId}:{encodedPayload}";
			            var urlEncodedPayload = HttpUtility.UrlEncode(resetPasswordToken, Encoding.ASCII);
			            var resetPasswordLink = $"https://{forgotPasswordRequest.ClearHost}/set-password?token={urlEncodedPayload}";
			            // SendMail !!!

			            return this.Ok();
		            }
		            else
		            {
			            try
			            {
				            var errorModel = JsonConvert.DeserializeObject<ErrorModel>(resetPasswordResponse.Message);
				            if (errorModel != null)
				            {
					            if (errorModel.Message.StartsWith("User not found in db by given username or email_address"))
					            {
						            return this.BadRequest($"User not found ({forgotPasswordRequest.EmailAddress})");
					            }
        					
					            return this.BadRequest(errorModel.Message);	
				            }
				            else
				            {
					            return this.BadRequest(resetPasswordResponse.Message);
				            }
			            }
			            catch
			            {
				            return this.BadRequest(resetPasswordResponse.Message);	
			            }
		            }
	            }
	        }
	        catch (Exception ex)
	        {
		        return this.BadRequest(ex.Message);
	        }
        }

        #endregion
    }
}