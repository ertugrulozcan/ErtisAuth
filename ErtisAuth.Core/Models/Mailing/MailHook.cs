using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Events;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Core.Models.Mailing;

public class MailHook : MembershipBoundedResource, IHasSysInfo
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
    
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }
	
    [JsonProperty("event")]
    [JsonPropertyName("event")]
    [BsonElement("event")]
    public string? Event { get; set; }
    
    [JsonIgnore]
    [BsonIgnore]
    [NewtonsoftJsonIgnore]
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
    [BsonElement("status")]
    public string? Status { get; set; }
    
    [JsonIgnore]
    [BsonIgnore]
    [NewtonsoftJsonIgnore]
    public bool IsActive => this.Status == "active";
    
    [JsonProperty("mailSubject")]
    [JsonPropertyName("mailSubject")]
    [BsonElement("mailSubject")]
    public string? MailSubject { get; set; }
    
    [JsonProperty("mailTemplate")]
    [JsonPropertyName("mailTemplate")]
    [BsonElement("mailTemplate")]
    public string? MailTemplate { get; set; }
    
    [JsonProperty("fromName")]
    [JsonPropertyName("fromName")]
    [BsonElement("fromName")]
    public string? FromName { get; set; }
    
    [JsonProperty("fromAddress")]
    [JsonPropertyName("fromAddress")]
    [BsonElement("fromAddress")]
    public string? FromAddress { get; set; }
    
    [JsonProperty("sendToUtilizer")]
    [JsonPropertyName("sendToUtilizer")]
    [BsonElement("sendToUtilizer")]
    public bool SendToUtilizer { get; set; }
    
    [JsonProperty("recipients")]
    [JsonPropertyName("recipients")]
    [BsonElement("recipients")]
    public Recipient[]? Recipients { get; set; }
    
    [JsonProperty("mailProvider")]
    [JsonPropertyName("mailProvider")]
    [BsonElement("mailProvider")]
    public string? MailProvider { get; set; }
    
    [JsonPropertyName("variables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonProperty("variables", NullValueHandling = NullValueHandling.Ignore)]
    [BsonElement("variables")]
    [BsonIgnoreIfNull]
    public MailHookVariable[]? Variables { get; set; }
    
    [JsonProperty("sys")]
    [JsonPropertyName("sys")]
    [BsonElement("sys")]
    public SysModel? Sys { get; set; }
    
    #endregion
}

public class MailHookVariable
{
    #region Properties
    
    [JsonProperty("key")]
    [JsonPropertyName("key")]
    [BsonElement("key")]
    public string? Key { get; set; }
    
    [JsonProperty("value")]
    [JsonPropertyName("value")]
    [BsonElement("value")]
    public string? Value { get; set; }
    
    #endregion
}