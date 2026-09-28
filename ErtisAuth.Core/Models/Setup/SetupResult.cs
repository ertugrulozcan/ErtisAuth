using System.Text.Json.Serialization;
using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Setup;

/// <summary>
/// The resources created by the setup; the application secret is returned only here, once.
/// </summary>
public class SetupResult
{
	#region Properties
	
	[JsonProperty("membership")]
	[JsonPropertyName("membership")]
	public required Membership Membership { get; init; }
	
	[JsonProperty("user")]
	[JsonPropertyName("user")]
	public required DynamicObject User { get; init; }
	
	[JsonProperty("role")]
	[JsonPropertyName("role")]
	public required Role Role { get; init; }
	
	[JsonProperty("application", NullValueHandling = NullValueHandling.Ignore)]
	[JsonPropertyName("application")]
	[System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ApplicationWithSecret? Application { get; init; }
	
	#endregion
}