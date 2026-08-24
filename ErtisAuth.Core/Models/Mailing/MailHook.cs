using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Extensions.Mailkit.Models;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Mailing;

public class MailHook : MembershipBoundedResource, IHasSysInfo
{
    #region Properties
    
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonProperty("slug")]
    [JsonPropertyName("slug")]
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
    public string? Description { get; set; }
	
    [JsonProperty("event")]
    [JsonPropertyName("event")]
    public string? Event { get; set; }
    
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public ErtisAuthEventType? EventType
    {
        get
        {
            if (string.IsNullOrEmpty(this.Event))
            {
                return null;
            }
            
            if (Enum.GetNames(typeof(ErtisAuthEventType)).Any(x => x == this.Event))
            {
                return (ErtisAuthEventType) Enum.Parse(typeof(ErtisAuthEventType), this.Event);
            }
            else
            {
                return null;
            }
        }
    }
    
    [JsonProperty("status")]
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsActive => this.Status == "active";
    
    [JsonProperty("mailSubject")]
    [JsonPropertyName("mailSubject")]
    public string? MailSubject { get; set; }
    
    [JsonProperty("mailTemplate")]
    [JsonPropertyName("mailTemplate")]
    public string? MailTemplate { get; set; }
    
    [JsonProperty("fromName")]
    [JsonPropertyName("fromName")]
    public string? FromName { get; set; }
    
    [JsonProperty("fromAddress")]
    [JsonPropertyName("fromAddress")]
    public string? FromAddress { get; set; }
    
    [JsonProperty("sendToUtilizer")]
    [JsonPropertyName("sendToUtilizer")]
    public bool SendToUtilizer { get; set; }
    
    [JsonProperty("recipients")]
    [JsonPropertyName("recipients")]
    public Recipient[]? Recipients { get; set; }
    
    [JsonProperty("mailProvider")]
    [JsonPropertyName("mailProvider")]
    public string? MailProvider { get; set; }
    
    [JsonProperty("variables")]
    [JsonPropertyName("variables")]
    public MailHookVariable[]? Variables { get; set; }
    
    [JsonProperty("sys")]
    [JsonPropertyName("sys")]
    public SysModel? Sys { get; set; }
    
    #endregion
}

public class MailHookVariable
{
    #region Properties
    
    [JsonProperty("key")]
    [JsonPropertyName("key")]
    public string? Key { get; set; }
    
    [JsonProperty("value")]
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    #endregion
}