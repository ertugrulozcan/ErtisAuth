using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.Core.Models.Mailing;

public class MailChimpProvider : IMailProvider
{
    #region Properties
    
    [JsonProperty("guid")]
    [JsonPropertyName("guid")]
    [BsonElement("guid")]
    public string? Guid { get; set; }
	
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    [NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MailProviderType Type => MailProviderType.MailChimp;
    
    [JsonProperty("deliveryMode")]
    [JsonPropertyName("deliveryMode")]
    [BsonElement("deliveryMode")]
    [NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public DeliveryMode DeliveryMode => DeliveryMode.Template;
	
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
    }
	
    [JsonProperty("apiKey")]
    [JsonPropertyName("apiKey")]
    [BsonElement("apiKey")]
    public string? ApiKey { get; set; }
    
    #endregion
    
    #region Methods
    
    public Task SendMailAsync(
        string fromName,
        string fromAddress,
        IEnumerable<Recipient> recipients,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
    public Task SendMailWithTemplateAsync(
        string fromName,
        string fromAddress,
        IEnumerable<Recipient> recipients,
        string subject,
        string templateId,
        IDictionary<string, string> arguments,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
    /*
    public Task SendMailAsync(
        string fromName, 
        string fromAddress, 
        IEnumerable<Recipient> recipients, 
        string subject, 
        string htmlBody, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("This provider is not supported with raw mailing");
    }
    
    public async Task SendMailWithTemplateAsync(
        string fromName, 
        string fromAddress, 
        IEnumerable<Recipient> recipients, 
        string subject, 
        string templateId, 
        IDictionary<string, string> arguments, 
        CancellationToken cancellationToken = default)
    {
        if (MailkitExtensions.RestHandler == null)
        {
            return;
        }
        
        var response = await MailkitExtensions.RestHandler.ExecuteRequestAsync(
            HttpMethod.Post,
            "https://mandrillapp.com/api/1.0/messages/send-template",
            HeaderCollection.Empty,
            new SystemJsonRequestBody(new MailChimpTemplatePayload
            {
                Key = this.ApiKey,
                TemplateName = templateId,
                TemplateContent = arguments.Select(x => new MailChimpTemplateContentItem
                {
                    Name = x.Key,
                    Content = x.Value
                }).ToArray(),
                Message = new MailChimpMessage
                {
                    Subject = subject,
                    FromEmail = fromAddress,
                    FromName = fromName,
                    To = recipients.Select(x => new MailChimpRecipient
                    {
                        Email = x.EmailAddress,
                        Name = x.DisplayName,
                        Type = MailChimpRecipientType.to
                    }).ToArray(),
                    GlobalMergeVars = arguments.Select(x => new MailChimpMergeVariable
                    {
                        Name = x.Key,
                        Content = x.Value
                    }).ToArray()
                },
                Async = true
            }
        ), cancellationToken: cancellationToken);
    }
    */
    
    #endregion
}

public class MailChimpTemplatePayload
{
    #region Properties
    
    [JsonProperty("key")]
    [JsonPropertyName("key")]
    public string? Key { get; set; }
    
    [JsonProperty("template_name")]
    [JsonPropertyName("template_name")]
    public string? TemplateName { get; set; }
    
    [JsonProperty("template_content")]
    [JsonPropertyName("template_content")]
    public MailChimpTemplateContentItem[]? TemplateContent { get; set; }
    
    [JsonProperty("message")]
    [JsonPropertyName("message")]
    public MailChimpMessage? Message { get; set; }
    
    [JsonProperty("async")]
    [JsonPropertyName("async")]
    public bool Async { get; set; }
    
    #endregion
}

public class MailChimpTemplateContentItem
{
    #region Properties
    
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonProperty("content")]
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    #endregion
}

public class MailChimpMessage
{
    #region Properties
    
    [JsonProperty("html")]
    [JsonPropertyName("html")]
    public string? Html { get; set; }
    
    [JsonProperty("text")]
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonProperty("subject")]
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }
    
    [JsonProperty("from_email")]
    [JsonPropertyName("from_email")]
    public string? FromEmail { get; set; }
    
    [JsonProperty("from_name")]
    [JsonPropertyName("from_name")]
    public string? FromName { get; set; }
    
    [JsonProperty("to")]
    [JsonPropertyName("to")]
    public MailChimpRecipient[]? To { get; set; }
    
    [JsonProperty("global_merge_vars")]
    [JsonPropertyName("global_merge_vars")]
    public MailChimpMergeVariable[]? GlobalMergeVars { get; set; }
    
    #endregion
}

public class MailChimpRecipient
{
    #region Properties
    
    [JsonProperty("email")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
    public MailChimpRecipientType Type { get; set; }
    
    #endregion
}

public class MailChimpMergeVariable
{
    #region Properties
    
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonProperty("content")]
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    #endregion
}

public enum MailChimpRecipientType
{
    to,
    cc,
    bcc
}