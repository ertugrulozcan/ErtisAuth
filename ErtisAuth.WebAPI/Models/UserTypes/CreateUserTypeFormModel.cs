using System.Text.Json.Serialization;
using Ertis.Schema.Serialization;
using Ertis.Schema.Types;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.UserTypes;

/// <summary>
/// The create request of a user type; the id and the membership come from the route.
/// </summary>
public class CreateUserTypeFormModel
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
}