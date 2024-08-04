using System;
using ErtisAuth.Extensions.Mailkit.Providers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ErtisAuth.Extensions.Mailkit.Serialization;

public class MailProviderJsonConverter : JsonConverter<IMailProvider>
{
	public override void WriteJson(JsonWriter writer, IMailProvider value, JsonSerializer serializer)
	{
		var jToken = JToken.FromObject(value);
		jToken.WriteTo(writer);
	}

	public override IMailProvider ReadJson(JsonReader reader, Type objectType, IMailProvider existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var jObject = JObject.Load(reader);
		return Deserialize(jObject);
	}
	
	public static IMailProvider Deserialize(string json)
	{
		return string.IsNullOrEmpty(json) ? null : Deserialize(JObject.Parse(json));
	}

	private static IMailProvider Deserialize(JObject jObject)
	{
		if (jObject.ContainsKey("type"))
		{
			var providerTypeName = jObject["type"]?.Value<string>();
			if (Enum.TryParse(providerTypeName, out MailProviderType mailProviderType))
			{
				var json = jObject.ToString(Formatting.None);
				IMailProvider mailProvider = mailProviderType switch
				{
					MailProviderType.SmtpServer => JsonConvert.DeserializeObject<SmtpServerProvider>(json),
					MailProviderType.SendGrid => JsonConvert.DeserializeObject<SendGridProvider>(json),
					MailProviderType.MailChimp => JsonConvert.DeserializeObject<MailChimpProvider>(json),
					_ => throw new Exception($"Unknown mail provider type : '{providerTypeName}'")
				};
                
				return mailProvider;
			}
			else
			{
				throw new Exception($"Unknown mail provider type : '{providerTypeName}'");
			}
		}
		else
		{
			throw new Exception($"Mail provider type is required");
		}
	}
}