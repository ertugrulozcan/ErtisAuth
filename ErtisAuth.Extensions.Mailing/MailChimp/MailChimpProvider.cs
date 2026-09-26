using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Mailing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using RecipientModel = ErtisAuth.Core.Models.Mailing.Recipient;
using MailChimpRecipient = ErtisAuth.Extensions.Mailing.MailChimp.Recipient;
using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.Extensions.Mailing.MailChimp;

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
        ISystemRestHandler restHandler,
        string fromName, 
        string fromAddress, 
        IEnumerable<RecipientModel> recipients, 
        string subject, 
        string htmlBody, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("This provider is not supported with raw mailing");
    }
    
    public async Task SendMailWithTemplateAsync(
        ISystemRestHandler restHandler,
        string fromName, 
        string fromAddress, 
        IEnumerable<RecipientModel> recipients, 
        string subject, 
        string templateId, 
        IDictionary<string, string> arguments, 
        CancellationToken cancellationToken = default)
    {
        await restHandler.ExecuteRequestAsync(
            HttpMethod.Post,
            "https://mandrillapp.com/api/1.0/messages/send-template",
            HeaderCollection.Empty,
            new SystemJsonRequestBody(new TemplatePayload
            {
                Key = this.ApiKey,
                TemplateName = templateId,
                TemplateContent = arguments.Select(x => new TemplateContentItem
                {
                    Name = x.Key,
                    Content = x.Value
                }).ToArray(),
                Message = new Message
                {
                    Subject = subject,
                    FromEmail = fromAddress,
                    FromName = fromName,
                    To = recipients.Select(x => new MailChimpRecipient
                    {
                        Email = x.EmailAddress,
                        Name = x.DisplayName,
                        Type = RecipientType.to
                    }).ToArray(),
                    GlobalMergeVars = arguments.Select(x => new Variable
                    {
                        Name = x.Key,
                        Content = x.Value
                    }).ToArray()
                },
                Async = true
            }
        ), cancellationToken: cancellationToken);
    }
    
    #endregion
}