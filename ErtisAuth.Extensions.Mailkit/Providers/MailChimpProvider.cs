using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Helpers;
using Ertis.Net.Http;
using ErtisAuth.Extensions.Mailkit.Extensions;
using ErtisAuth.Extensions.Mailkit.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Extensions.Mailkit.Providers;

public class MailChimpProvider : IMailProvider
{
    #region Fields

    private string slug;
	
    #endregion
    
    #region Properties
    
    [JsonProperty("guid")]
    [JsonPropertyName("guid")]
    public string Guid { get; set; }
	
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
    public MailProviderType Type => MailProviderType.MailChimp;
    
    [JsonProperty("deliveryMode")]
    [JsonPropertyName("deliveryMode")]
    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
    public MailDeliveryMode DeliveryMode => MailDeliveryMode.Template;
	
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
    }
	
    [JsonProperty("apiKey")]
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; }

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
            Console.WriteLine("MailkitExtensions.RestHandler is null. MailChimp template mail could not be sent");
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

        if (response.IsSuccess)
        {
            Console.WriteLine("MailChimp template mail sent");
        }
        else
        {
            Console.WriteLine("MailChimp template mail could not be sent");
            Console.WriteLine(response.Message);
        }
    }
    
    #endregion
}