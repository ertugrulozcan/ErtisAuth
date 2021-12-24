using System.Collections.Generic;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Users
{
	public class UpdateUserFormModel
	{
		#region Properties

		[JsonProperty("username")]
		public string Username { get; set; }
		
		[JsonProperty("email_address")]
		public string EmailAddress { get; set; }
		
		[JsonProperty("firstname")]
		public string FirstName { get; set; }
		
		[JsonProperty("lastname")]
		public string LastName { get; set; }
		
		[JsonProperty("role")]
		public string Role { get; set; }
		
		[JsonProperty("additional_properties")]
		public dynamic AdditionalProperties { get; set; }
		
		[JsonProperty("permissions")]
		public IEnumerable<string> Permissions { get; set; }
		
		[JsonProperty("forbidden")]
		public IEnumerable<string> Forbidden { get; set; }
		
		#endregion
	}
}