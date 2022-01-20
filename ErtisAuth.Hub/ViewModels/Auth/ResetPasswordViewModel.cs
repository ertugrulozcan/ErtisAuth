using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Web;

namespace ErtisAuth.Hub.ViewModels.Auth
{
    public class ResetPasswordViewModel : ViewModelBase
    {
        #region Properties

        public string EmailAddress { get; set; }
		
        [Required(ErrorMessage = "Please enter a new password")]
        [StringLength(int.MaxValue, MinimumLength = 6, ErrorMessage = "Password is at least 6 characters")]
        public string NewPassword { get; set; }
		
        [Required(ErrorMessage = "Please re-enter new password")]
        [StringLength(int.MaxValue, MinimumLength = 6, ErrorMessage = "Password is at least 6 characters")]
        [CompareAttribute("NewPassword", ErrorMessage = "Passwords does not match")]
        public string NewPasswordAgain { get; set; }
        
        public string ServerUrl { get; set; }
        
        public string SecretKey { get; set; }
        
        public string ResetPasswordToken { get; set; }
        
        public string MembershipId { get; set; }

        #endregion

        #region Methods

        public static ResetPasswordViewModel Parse(string token)
        {
	        if (TryParse(token, out var resetPasswordViewModel))
	        {
		        return resetPasswordViewModel;
	        }
	        else
	        {
		        return new ResetPasswordViewModel
		        {
			        IsSuccess = false,
			        ErrorMessage = "Your reset password token has expired or invalid, please again reset your password"
		        };
	        }
        }
        
        private static bool TryParse(string token, out ResetPasswordViewModel resetPasswordViewModel)
        {
	        if (!string.IsNullOrEmpty(token))
            {
            	var urlEncodedToken = token;
            	var tokenPackage = HttpUtility.UrlDecode(urlEncodedToken, Encoding.ASCII);
            	var parts = tokenPackage.Split(':');
            	if (parts.Length > 1)
            	{
            		var membershipId = parts[0];
            		var encodedPayload = tokenPackage.Substring(membershipId.Length + 1);
            		var payload = Identity.Cryptography.StringCipher.Decrypt(encodedPayload, membershipId);
            		var segments = payload.Split('&');

            		var payloadDictionary = new Dictionary<string, string>();
            		foreach (var segment in segments)
            		{
            			var pairs = segment.Split('=');
            			var key = pairs[0];
            			var value = segment.Substring(key.Length + 1);
            			payloadDictionary.Add(key, value);
            		}

            		if (!payloadDictionary.ContainsKey("emailAddress") ||
                        !payloadDictionary.ContainsKey("encryptedSecretKey") ||
                        !payloadDictionary.ContainsKey("serverUrl") ||
                        !payloadDictionary.ContainsKey("membershipId") ||
                        !payloadDictionary.ContainsKey("encryptedResetPasswordToken") ||
                        membershipId != payloadDictionary["membershipId"])
                    {
	                    resetPasswordViewModel = null;
	                    return false;
                    }

                    var emailAddress = payloadDictionary["emailAddress"];
                    var serverUrl = payloadDictionary["serverUrl"];
                    var encryptedSecretKey = payloadDictionary["encryptedSecretKey"];
                    var secretKey = Identity.Cryptography.StringCipher.Decrypt(encryptedSecretKey, membershipId);
                    var encryptedResetPasswordToken = payloadDictionary["encryptedResetPasswordToken"];
                    var resetPasswordToken = Identity.Cryptography.StringCipher.Decrypt(encryptedResetPasswordToken, membershipId);

                    resetPasswordViewModel = new ResetPasswordViewModel
                    {
	                    EmailAddress = emailAddress,
	                    ServerUrl = serverUrl,
	                    SecretKey = secretKey,
	                    ResetPasswordToken = resetPasswordToken,
	                    MembershipId = membershipId
                    };

                    return true;
                }
            	else
            	{
	                resetPasswordViewModel = null;
	                return false;
            	}
            }
            else
            {
	            resetPasswordViewModel = null;
	            return false;
            }
        }

        public bool IsValid()
        {
	        return
		        !string.IsNullOrEmpty(this.EmailAddress) &&
		        !string.IsNullOrEmpty(this.ServerUrl) &&
		        !string.IsNullOrEmpty(this.SecretKey) &&
		        !string.IsNullOrEmpty(this.ResetPasswordToken) &&
		        !string.IsNullOrEmpty(this.MembershipId);
        }

        #endregion
    }
}