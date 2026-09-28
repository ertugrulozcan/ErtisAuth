using System.Text.Json.Serialization;
using Ertis.Schema.Serialization;
using Ertis.Schema.Types;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.UserTypes;

/// <summary>
/// The update request of a user type; the id and the membership come from the route.
/// </summary>
public class UpdateUserTypeFormModel
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonProperty("description")]
	[JsonPropertyName("description")]
	public string? Description { get; set; }
	
	[JsonProperty("properties")]
	[JsonPropertyName("properties")]
	[System.Text.Json.Serialization.JsonConverter(typeof(FieldInfoCollectionJsonConverterFactory))]
	[Newtonsoft.Json.JsonConverter(typeof(Ertis.Schema.Serialization.Legacy.FieldInfoCollectionJsonConverter))]
	public IReadOnlyCollection<IFieldInfo>? Properties { get; set; }
	
	[JsonProperty("allowAdditionalProperties")]
	[JsonPropertyName("allowAdditionalProperties")]
	public bool AllowAdditionalProperties { get; set; }
	
	[JsonProperty("isAbstract")]
	[JsonPropertyName("isAbstract")]
	public bool IsAbstract { get; set; }
	
	[JsonProperty("isSealed")]
	[JsonPropertyName("isSealed")]
	public bool IsSealed { get; set; }
	
	[JsonProperty("baseType")]
	[JsonPropertyName("baseType")]
	public string? BaseUserType { get; set; }
	
	#endregion
}