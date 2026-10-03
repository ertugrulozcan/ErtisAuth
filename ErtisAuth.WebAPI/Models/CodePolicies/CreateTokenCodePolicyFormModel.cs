using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.CodePolicies;

/// <summary>
/// The create request of a token code policy; the id and the membership come from the route.
/// </summary>
public class CreateTokenCodePolicyFormModel
{
	#region Properties
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonPropertyName("description")]
	public string? Description { get; set; }
	
	[JsonPropertyName("length")]
	public int Length { get; set; }
	
	[JsonPropertyName("contains_letters")]
	public bool ContainsLetters { get; set; }
	
	[JsonPropertyName("contains_digits")]
	public bool ContainsDigits { get; set; }
	
	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }
	
	#endregion
	
	#region Methods
	
	internal TokenCodePolicy ToTokenCodePolicy(string membershipId)
	{
		var policy = new TokenCodePolicy
		{
			Name = this.Name ?? string.Empty,
			Description = this.Description,
			Length = this.Length,
			ContainsLetters = this.ContainsLetters,
			ContainsDigits = this.ContainsDigits,
			ExpiresIn = this.ExpiresIn,
			MembershipId = membershipId
		};
		
		// Otherwise derived from the name
		if (!string.IsNullOrEmpty(this.Slug))
		{
			policy.Slug = this.Slug;
		}
		
		return policy;
	}
	
	#endregion
}