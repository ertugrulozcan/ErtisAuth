using System.Collections.Generic;
using System.Text.Json.Serialization;
using Ertis.Core.Models.Resources;
using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Users
{
	public class User : MembershipBoundedResource, IUtilizer, IHasSysInfo
	{
		#region Properties

		[JsonProperty("firstname")]
		[JsonPropertyName("firstname")]
		public string FirstName { get; set; }
		
		[JsonProperty("lastname")]
		[JsonPropertyName("lastname")]
		public string LastName { get; set; }
		
		[JsonProperty("username")]
		[JsonPropertyName("username")]
		public string Username { get; set; }
		
		[JsonProperty("email_address")]
		[JsonPropertyName("email_address")]
		public string EmailAddress { get; set; }
		
		[JsonProperty("role")]
		[JsonPropertyName("role")]
		public string Role { get; set; }
		
		[JsonProperty("permissions")]
		[JsonPropertyName("permissions")]
		public IEnumerable<string> Permissions { get; set; }
		
		[JsonProperty("forbidden")]
		[JsonPropertyName("forbidden")]
		public IEnumerable<string> Forbidden { get; set; }

		[JsonProperty("user_type")]
		[JsonPropertyName("user_type")]
		public string UserType { get; set; }
		
		[JsonProperty("source_provider")]
		[JsonPropertyName("source_provider")]
		public string SourceProvider { get; set; }
		
		[JsonProperty("connected_accounts")]
		[JsonPropertyName("connected_accounts")]
		public ProviderAccountInfo[] ConnectedAccounts { get; set; }
		
		[JsonProperty("is_active")]
		[JsonPropertyName("is_active")]
		public bool IsActive { get; set; }
		
		[JsonProperty("sys")]
		[JsonPropertyName("sys")]
		public SysModel Sys { get; set; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public Utilizer.UtilizerType UtilizerType => Utilizer.UtilizerType.User;
		
		#endregion

		#region Implicit & Explicit Operators

		public static implicit operator DynamicObject(User user) => new(user);
		
		public static explicit operator User(DynamicObject dynamicObject) => dynamicObject.Deserialize<User>();

		#endregion
	}
}