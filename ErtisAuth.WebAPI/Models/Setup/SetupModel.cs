using System.Text.Json.Serialization;
using ErtisAuth.WebAPI.Models.Applications;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Setup;

/// <summary>
/// The setup request. Not the domain models: the membership secret is generated when omitted, the user always gets the
/// administrator role and the new membership.
/// </summary>
public class SetupModel
{
	#region Properties
	
	[JsonProperty("membership")]
	[JsonPropertyName("membership")]
	public SetupMembershipModel? Membership { get; set; }
	
	[JsonProperty("user")]
	[JsonPropertyName("user")]
	public SetupUserModel? User { get; set; }
	
	[JsonProperty("application")]
	[JsonPropertyName("application")]
	public CreateApplicationFormModel? Application { get; set; }
	
	#endregion
}