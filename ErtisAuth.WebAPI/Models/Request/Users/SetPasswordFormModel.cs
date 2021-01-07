using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Users
{
	public class SetPasswordFormModel
	{
		#region Properties

		[JsonProperty("email_address")]
		public string EmailAddress { get; set; }
		
		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("username")]
		public string Username { get; set; }
		
		[JsonProperty("reset_token")]
		public string ResetToken { get; set; }
		
		[JsonProperty("password")]
		public string Password { get; set; }

		[JsonIgnore]
		public string UsernameOrEmailAddress
		{
			get
			{
				if (!string.IsNullOrEmpty(this.EmailAddress))
				{
					return this.EmailAddress;
				}
				else if (!string.IsNullOrEmpty(this.Email))
				{
					return this.Email;
				}
				else if (!string.IsNullOrEmpty(this.Username))
				{
					return this.Username;
				}

				return null;
			}
		}
		
		#endregion

		#region Methods

		public override string ToString()
		{
			return this.UsernameOrEmailAddress;
		}

		#endregion
	}
}