using System.Text.Json.Serialization;
using Ertis.Schema.Serialization;
using Ertis.Schema.Types;
using ErtisAuth.Core.Models.Users;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.UserTypes;

/// <summary>
/// The update request of a user type; the id and the membership come from the route.
/// </summary>
public class UpdateUserTypeFormModel
{
	#region Properties
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonPropertyName("description")]
	public string? Description { get; set; }
	
	[JsonPropertyName("properties")]
	[JsonConverter(typeof(FieldInfoCollectionJsonConverterFactory))]
	public IReadOnlyCollection<IFieldInfo>? Properties { get; set; }
	
	[JsonPropertyName("allowAdditionalProperties")]
	public bool AllowAdditionalProperties { get; set; }
	
	[JsonPropertyName("isAbstract")]
	public bool IsAbstract { get; set; }
	
	[JsonPropertyName("isSealed")]
	public bool IsSealed { get; set; }
	
	[JsonPropertyName("baseType")]
	public string? BaseUserType { get; set; }
	
	#endregion
	
	#region Methods
	
	internal UserType ToUserType(string id, string membershipId)
	{
		var userType = new UserType
		{
			Id = id,
			Name = this.Name ?? string.Empty,
			Description = this.Description,
			Properties = this.Properties ?? [],
			AllowAdditionalProperties = this.AllowAdditionalProperties,
			IsAbstract = this.IsAbstract,
			IsSealed = this.IsSealed,
			BaseUserType = this.BaseUserType,
			MembershipId = membershipId
		};
		
		// Otherwise derived from the name
		if (!string.IsNullOrEmpty(this.Slug))
		{
			userType.Slug = this.Slug;
		}
		
		return userType;
	}
	
	#endregion
}