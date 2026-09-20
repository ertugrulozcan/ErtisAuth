using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.WebAPI.Models.Applications;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Migration;

public class MigrationModel
{
	#region Properties
	
	[JsonProperty("membership")]
	[JsonPropertyName("membership")]
	public Membership? Membership { get; set; }
	
	[JsonProperty("user")]
	[JsonPropertyName("user")]
	public UserWithPassword? User { get; set; }
	
	[JsonProperty("application")]
	[JsonPropertyName("application")]
	public CreateApplicationFormModel? Application { get; set; }
	
	#endregion
}