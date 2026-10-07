using System.Text.Json;
using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Extensions.Mailing.MailChimp;
using ErtisAuth.Extensions.Mailing.SendGrid;
using ErtisAuth.Extensions.Mailing.SmtpServer;

namespace ErtisAuth.Extensions.Mailing.Serialization;

/// <summary>
/// Picks the concrete mail provider by its 'type' (case-sensitive, same values as the BSON discriminator).
/// Only IMailProvider itself is handled here: the concrete types go through the default serialization,
/// otherwise serializing or deserializing them with the same options would re-enter this converter.
/// Invalid input throws JsonException, which the API turns into 400.
/// </summary>
public class MailProviderJsonConverter : JsonConverter<IMailProvider>
{
	#region Methods
	
	public override IMailProvider Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		var element = document.RootElement;
		if (element.ValueKind != JsonValueKind.Object)
		{
			throw new JsonException("Mail provider must be an object");
		}
		
		if (!element.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(typeElement.GetString()))
		{
			throw new JsonException("Mail provider type required");
		}
		
		var typeName = typeElement.GetString();
		var type = typeName switch
		{
			nameof(MailProviderType.SmtpServer) => typeof(SmtpServerProvider),
			nameof(MailProviderType.SendGrid) => typeof(SendGridProvider),
			nameof(MailProviderType.MailChimp) => typeof(MailChimpProvider),
			_ => throw new JsonException($"Unknown mail provider type: '{typeName}'")
		};
		
		return (IMailProvider) element.Deserialize(type, options)!;
	}
	
	public override void Write(Utf8JsonWriter writer, IMailProvider mailProvider, JsonSerializerOptions options) =>
		JsonSerializer.Serialize(writer, mailProvider, mailProvider.GetType(), options);
	
	#endregion
}