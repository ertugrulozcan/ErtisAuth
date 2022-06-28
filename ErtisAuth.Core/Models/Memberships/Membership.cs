using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Users;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Memberships
{
	public class Membership : ResourceBase, IHasSysInfo
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
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

		[JsonProperty("mail_settings")]
		public MembershipMailSettings MailSettings { get; set; }

		[JsonProperty("sys")]
		public SysModel Sys { get; set; }
		
		#endregion
	}
}