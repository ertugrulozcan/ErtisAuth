using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Memberships
{
	public class CreateMembershipFormModel
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("slug")]
		public string Slug { get; set; }
		
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

		#endregion

		#region Methods

		public Membership ToMembership()
		{
			return new Membership
			{
				Name = this.Name,
				Slug = this.Slug,
				ExpiresIn = this.ExpiresIn,
				RefreshTokenExpiresIn = this.RefreshTokenExpiresIn,
				SecretKey = this.SecretKey,
				HashAlgorithm = this.HashAlgorithm,
				DefaultEncoding = this.DefaultEncoding,
				DefaultLanguage = this.DefaultLanguage,
			};
		}

		#endregion
	}
}