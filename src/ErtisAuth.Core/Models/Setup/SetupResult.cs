using System.Text.Json.Serialization;
using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Core.Models.Setup;

/// <summary>
/// The resources created by the setup; the application secret is returned only here, once.
/// </summary>
public class SetupResult
{
	#region Properties
	
	[JsonPropertyName("membership")]
	public required Membership Membership { get; init; }
	
	[JsonPropertyName("user")]
	public required DynamicObject User { get; init; }
	
	[JsonPropertyName("role")]
	public required Role Role { get; init; }
	
	[JsonPropertyName("application")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ApplicationWithSecret? Application { get; init; }
	
	#endregion
}