using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Extensions.Mailkit.Models;

public class MailChimpTemplatePayload
{
    #region Properties

    [JsonProperty("key")]
    [JsonPropertyName("key")]
    public string Key { get; set; }
    
    [JsonProperty("template_name")]
    [JsonPropertyName("template_name")]
    public string TemplateName { get; set; }
    
    [JsonProperty("template_content")]
    [JsonPropertyName("template_content")]
    public MailChimpTemplateContentItem[] TemplateContent { get; set; }
    
    [JsonProperty("message")]
    [JsonPropertyName("message")]
    public MailChimpMessage Message { get; set; }
    
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
    public string Name { get; set; }
    
    [JsonProperty("content")]
    [JsonPropertyName("content")]
    public string Content { get; set; }
    
    #endregion
}

public class MailChimpMessage
{
    #region Properties

    [JsonProperty("html")]
    [JsonPropertyName("html")]
    public string Html { get; set; }

    [JsonProperty("text")]
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonProperty("subject")]
    [JsonPropertyName("subject")]
    public string Subject { get; set; }

    [JsonProperty("from_email")]
    [JsonPropertyName("from_email")]
    public string FromEmail { get; set; }

    [JsonProperty("from_name")]
    [JsonPropertyName("from_name")]
    public string FromName { get; set; }
    
    [JsonProperty("to")]
    [JsonPropertyName("to")]
    public MailChimpRecipient[] To { get; set; }
    
    [JsonProperty("global_merge_vars")]
    [JsonPropertyName("global_merge_vars")]
    public MailChimpMergeVariable[] GlobalMergeVars { get; set; }
    
    #endregion
}

public class MailChimpRecipient
{
    #region Properties

    [JsonProperty("email")]
    [JsonPropertyName("email")]
    public string Email { get; set; }
    
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
    public MailChimpRecipientType Type { get; set; }

    #endregion
}

public class MailChimpMergeVariable
{
    #region Properties

    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonProperty("content")]
    [JsonPropertyName("content")]
    public string Content { get; set; }
    
    #endregion
}

public enum MailChimpRecipientType
{
    to,
    cc,
    bcc
}