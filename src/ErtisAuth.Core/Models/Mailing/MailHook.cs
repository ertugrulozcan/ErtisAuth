using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models;
using ErtisAuth.Core.Models.Events;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Mailing;

public class MailHook : MembershipBoundedResource, IHasSlug, IHasSysInfo
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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }
    
    [JsonPropertyName("event")]
    [BsonElement("event")]
    public string? Event { get; set; }
    
    [JsonIgnore]
    [BsonIgnore]
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
    
    [JsonPropertyName("status")]
    [BsonElement("status")]
    public string? Status { get; set; }
    
    [JsonIgnore]
    [BsonIgnore]
    public bool IsActive => this.Status == "active";
    
    [JsonPropertyName("mailSubject")]
    [BsonElement("mailSubject")]
    public string? MailSubject { get; set; }
    
    [JsonPropertyName("mailTemplate")]
    [BsonElement("mailTemplate")]
    public string? MailTemplate { get; set; }
    
    [JsonPropertyName("fromName")]
    [BsonElement("fromName")]
    public string? FromName { get; set; }
    
    [JsonPropertyName("fromAddress")]
    [BsonElement("fromAddress")]
    public string? FromAddress { get; set; }
    
    [JsonPropertyName("sendToUtilizer")]
    [BsonElement("sendToUtilizer")]
    public bool SendToUtilizer { get; set; }
    
    [JsonPropertyName("recipients")]
    [BsonElement("recipients")]
    public Recipient[]? Recipients { get; set; }
    
    [JsonPropertyName("mailProvider")]
    [BsonElement("mailProvider")]
    public string? MailProvider { get; set; }
    
    [JsonPropertyName("variables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonElement("variables")]
    [BsonIgnoreIfNull]
    public MailHookVariable[]? Variables { get; set; }
    
    [JsonPropertyName("sys")]
    [BsonElement("sys")]
    public SysModel? Sys { get; set; }
    
    #endregion
}

public class MailHookVariable
{
    #region Properties
    
    [JsonPropertyName("key")]
    [BsonElement("key")]
    public string? Key { get; set; }
    
    [JsonPropertyName("value")]
    [BsonElement("value")]
    public string? Value { get; set; }
    
    #endregion
}