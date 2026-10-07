using System.Text.Json;
using System.Text.Json.Serialization;

namespace ErtisAuth.Core.Serialization;

/// <summary>
/// Writes the [EnumMember] value (e.g. "active"),
/// reads the [EnumMember] value and the enum name case-insensitively. JsonStringEnumConverter ignores [EnumMember],
/// and JsonStringEnumMemberName would reject "Active".
/// </summary>
public sealed class EnumMemberJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
	#region Methods
	
	public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
		if (EnumMemberNames<TEnum>.TryParse(text, out var value))
		{
			return value;
		}
		
		throw new JsonException($"'{text}' is not a valid {typeof(TEnum).Name} value.");
	}
	
	public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(EnumMemberNames<TEnum>.GetName(value));
	}
	
	#endregion
}