using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class TokenCodePolicy : MembershipBoundedResource, IHasSysInfo
{
    #region Fields

    private string slug;

    #endregion
    
    #region Properties
    
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }
		
    [JsonProperty("slug")]
    [JsonPropertyName("slug")]
    public string Slug
    {
        get
        {
            if (string.IsNullOrEmpty(this.slug))
            {
                this.slug = Slugifier.Slugify(this.Name, Slugifier.Options.Ignore('_'));
            }

            return this.slug;
        }
        set => this.slug = Slugifier.Slugify(value, Slugifier.Options.Ignore('_'));
    }
    
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonProperty("length")]
    [JsonPropertyName("length")]
    public int Length { get; set; }
    
    [JsonProperty("contains_letters")]
    [JsonPropertyName("contains_letters")]
    public bool ContainsLetters { get; set; }
    
    [JsonProperty("contains_digits")]
    [JsonPropertyName("contains_digits")]
    public bool ContainsDigits { get; set; }
    
    [JsonProperty("expires_in")]
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonProperty("sys")]
    [JsonPropertyName("sys")]
    public SysModel Sys { get; set; }
    
    #endregion
}