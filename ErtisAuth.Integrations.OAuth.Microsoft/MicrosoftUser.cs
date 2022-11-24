using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Microsoft
{
	public class MicrosoftUser : IProviderUser
	{
		#region Properties

		[JsonProperty("id")]
		public string Id { get; set; }
		
		[JsonProperty("displayName")]
		public string DisplayName { get; set; }
		
		[JsonProperty("givenName")]
		public string FirstName { get; set; }
		
		[JsonProperty("surname")]
		public string LastName { get; set; }
		
		[JsonProperty("userPrincipalName")]
		public string UserPrincipalName { get; set; }
		
		[JsonProperty("mail")]
		public string EmailAddress { get; set; }
		
		[JsonProperty("jobTitle")]
		public string JobTitle { get; set; }
		
		[JsonProperty("mobilePhone")]
		public string MobilePhone { get; set; }
		
		[JsonProperty("businessPhones")]
		public object[] BusinessPhones { get; set; }
		
		[JsonProperty("officeLocation")]
		public object OfficeLocation { get; set; }
		
		[JsonProperty("preferredLanguage")]
		public string PreferredLanguage { get; set; }
		
		[JsonProperty("photo")]
		public string Photo { get; set; }

		#endregion
	}
}