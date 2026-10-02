using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Identity;

public class TokenCodePolicy : MembershipBoundedResource, IHasSysInfo
{
    #region Properties
    
    [JsonPropertyName("name")]
    [BsonElement("name")]
    public required string Name { get; set; }
    
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
    
    [JsonPropertyName("description")]
    [BsonElement("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("length")]
    [BsonElement("length")]
    public int Length { get; set; }
    
    [JsonPropertyName("contains_letters")]
    [BsonElement("contains_letters")]
    public bool ContainsLetters { get; set; }
    
    [JsonPropertyName("contains_digits")]
    [BsonElement("contains_digits")]
    public bool ContainsDigits { get; set; }
    
    [JsonPropertyName("expires_in")]
    [BsonElement("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonPropertyName("sys")]
    [BsonElement("sys")]
    public SysModel? Sys { get; set; }
    
    #endregion
}