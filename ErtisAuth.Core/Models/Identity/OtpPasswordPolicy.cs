using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class OtpPasswordPolicy
{
    #region Properties

    [JsonProperty("length")]
    [JsonPropertyName("length")]
    public int Length { get; set; }
    
    [JsonProperty("contains_letters")]
    [JsonPropertyName("contains_letters")]
    public bool ContainsLetters { get; set; }
    
    [JsonProperty("contains_digits")]
    [JsonPropertyName("contains_digits")]
    public bool ContainsDigits { get; set; }

    #endregion
}