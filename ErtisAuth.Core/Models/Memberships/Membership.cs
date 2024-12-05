using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.Mailkit.Providers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Core.Models.Memberships
{
	public class Membership : ResourceBase, IHasSysInfo
	{
		#region Fields

		private string slug;

		#endregion
		
		#region Properties

		[JsonProperty("name")]
		[JsonPropertyName("name")]
		public string Name { get; set; }
		
		[JsonProperty("slug")]
		[JsonPropertyName("slug")]
		public string Slug
		{
			get
			{
				if (string.IsNullOrEmpty(this.slug))
				{
					this.slug = Slugifier.Slugify(this.Name, Slugifier.Options.Ignore('_'));
				}

				return this.slug;
			}
			set => this.slug = Slugifier.Slugify(value, Slugifier.Options.Ignore('_'));
		}
		
		[JsonProperty("expires_in")]
		[JsonPropertyName("expires_in")]
		public int ExpiresIn { get; set; }
		
		[JsonProperty("refresh_token_expires_in")]
		[JsonPropertyName("refresh_token_expires_in")]
		public int RefreshTokenExpiresIn { get; set; }
		
		[JsonProperty("reset_password_token_expires_in")]
		[JsonPropertyName("reset_password_token_expires_in")]
		public int? ResetPasswordTokenExpiresIn { get; set; }
		
		[JsonProperty("secret_key")]
		[JsonPropertyName("secret_key")]
		public string SecretKey { get; set; }
		
		[JsonProperty("hash_algorithm")]
		[JsonPropertyName("hash_algorithm")]
		public string HashAlgorithm { get; set; }

		[JsonProperty("encoding")]
		[JsonPropertyName("encoding")]
		public string DefaultEncoding { get; set; }
		
		[JsonProperty("default_language")]
		[JsonPropertyName("default_language")]
		public string DefaultLanguage { get; set; }

		[JsonProperty("mail_providers")]
		[JsonPropertyName("mail_providers")]
		public IMailProvider[] MailProviders { get; set; }
		
		[JsonProperty("user_activation")]
		[JsonPropertyName("user_activation")]
		[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
		[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
		public Status UserActivation { get; set; }
		
		[JsonProperty("code_policy")]
		[JsonPropertyName("code_policy")]
		public string CodePolicy { get; set; }
		
		[JsonProperty("otp_settings")]
		[JsonPropertyName("otp_settings")]
		public OtpSettings OtpSettings { get; set; }

		[JsonProperty("sys")]
		[JsonPropertyName("sys")]
		public SysModel Sys { get; set; }
		
		#endregion
	}
}