using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
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
		public string Name { get; set; }
		
		[JsonProperty("slug")]
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
		public int ExpiresIn { get; set; }
		
		[JsonProperty("refresh_token_expires_in")]
		public int RefreshTokenExpiresIn { get; set; }
		
		[JsonProperty("secret_key")]
		public string SecretKey { get; set; }
		
		[JsonProperty("hash_algorithm")]
		public string HashAlgorithm { get; set; }

		[JsonProperty("encoding")]
		public string DefaultEncoding { get; set; }
		
		[JsonProperty("default_language")]
		public string DefaultLanguage { get; set; }

		[JsonProperty("mail_providers")]
		public IMailProvider[] MailProviders { get; set; }
		
		[JsonProperty("user_activation")]
		[JsonConverter(typeof(StringEnumConverter))]
		public Status UserActivation { get; set; }
		
		[JsonProperty("code_policy")]
		public string CodePolicy { get; set; }

		[JsonProperty("sys")]
		public SysModel Sys { get; set; }
		
		#endregion
	}
}