using System.Text.Json.Serialization;
using ErtisAuth.WebAPI.Models.Applications;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Setup;

/// <summary>
/// The setup request. Not the domain models: the membership secret is generated when omitted, the user always gets the
/// administrator role and the new membership.
/// </summary>
public class SetupModel
{
	#region Properties
	
	[JsonPropertyName("membership")]
	public SetupMembershipModel? Membership { get; set; }
	
	[JsonPropertyName("user")]
	public SetupUserModel? User { get; set; }
	
	[JsonPropertyName("application")]
	public CreateApplicationFormModel? Application { get; set; }
	
	#endregion
}