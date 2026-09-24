using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class TokenCodePolicy : MembershipBoundedResource, IHasSysInfo
{
    #region Properties
    
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    [BsonElement("name")]
    public required string Name { get; set; }
    
    [JsonProperty("slug")]
    [JsonPropertyName("slug")]
    [BsonElement("slug")]
    public string Slug
    {
        get
        {
            if (string.IsNullOrEmpty(field))
            {
                field = Slugifier.Slugify(this.Name, Slugifier.Options.Ignore('_'));
            }
            
            return field;
        }
        set => field = Slugifier.Slugify(value, Slugifier.Options.Ignore('_'));
    }
    
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    [BsonElement("description")]
    public string? Description { get; set; }
    
    [JsonProperty("length")]
    [JsonPropertyName("length")]
    [BsonElement("length")]
    public int Length { get; set; }
    
    [JsonProperty("contains_letters")]
    [JsonPropertyName("contains_letters")]
    [BsonElement("contains_letters")]
    public bool ContainsLetters { get; set; }
    
    [JsonProperty("contains_digits")]
    [JsonPropertyName("contains_digits")]
    [BsonElement("contains_digits")]
    public bool ContainsDigits { get; set; }
    
    [JsonProperty("expires_in")]
    [JsonPropertyName("expires_in")]
    [BsonElement("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonProperty("sys")]
    [JsonPropertyName("sys")]
    [BsonElement("sys")]
    public SysModel? Sys { get; set; }
    
    #endregion
}