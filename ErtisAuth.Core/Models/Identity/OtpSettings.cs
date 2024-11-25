using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class OtpSettings
{
    #region Properties
    
    [JsonProperty("host")]
    [JsonPropertyName("host")]
    public string Host { get; set; }
    
    [JsonProperty("policy")]
    [JsonPropertyName("policy")]
    public OtpPasswordPolicy Policy { get; set; }

    #endregion
}