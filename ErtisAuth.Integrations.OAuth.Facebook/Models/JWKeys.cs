using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Facebook.Models;

public class JWKeys
{
    #region Properties
    
    [JsonProperty("keys")]
    [JsonPropertyName("keys")]
    public JWKey[] Keys { get; set; }

    #endregion
}

public class JWKey
{
    #region Properties

    [JsonProperty("kid")]
    [JsonPropertyName("kid")]
    public string Kid { get; set; }
    
    [JsonProperty("kty")]
    [JsonPropertyName("kty")]
    public string Kty { get; set; }
    
    [JsonProperty("alg")]
    [JsonPropertyName("alg")]
    public string Alg { get; set; }
    
    [JsonProperty("use")]
    [JsonPropertyName("use")]
    public string Use { get; set; }
    
    [JsonProperty("n")]
    [JsonPropertyName("n")]
    public string N { get; set; }
    
    [JsonProperty("e")]
    [JsonPropertyName("e")]
    public string E { get; set; }

    #endregion
}