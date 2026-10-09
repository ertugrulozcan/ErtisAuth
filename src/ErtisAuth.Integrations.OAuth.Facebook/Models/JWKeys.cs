using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Facebook.Models;

public class JWKeys
{
    #region Properties
    
    [JsonPropertyName("keys")]
    public JWKey[]? Keys { get; set; }
    
    #endregion
}

public class JWKey
{
    #region Properties
    
    [JsonPropertyName("kid")]
    public string? Kid { get; set; }
    
    [JsonPropertyName("kty")]
    public string? Kty { get; set; }
    
    [JsonPropertyName("alg")]
    public string? Alg { get; set; }
    
    [JsonPropertyName("use")]
    public string? Use { get; set; }
    
    [JsonPropertyName("n")]
    public string? N { get; set; }
    
    [JsonPropertyName("e")]
    public string? E { get; set; }
    
    #endregion
}