using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ErtisAuth.Core.Serialization;

/// <summary>
/// Stores an enum as its [EnumMember] value (e.g. "active"), as the DTO layer did before .NET 10.
/// Reads the [EnumMember] value and the enum name case-insensitively.
/// </summary>
public sealed class EnumMemberBsonSerializer<TEnum> : StructSerializerBase<TEnum> where TEnum : struct, Enum
{
	#region Methods
	
	public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum value)
	{
		context.Writer.WriteString(EnumMemberNames<TEnum>.GetName(value));
	}
	
	public override TEnum Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
	{
		var text = context.Reader.ReadString();
		if (EnumMemberNames<TEnum>.TryParse(text, out var value))
		{
			return value;
		}
		
		throw new FormatException($"'{text}' is not a valid {typeof(TEnum).Name} value.");
	}
	
	#endregion
}

/// <summary>
/// Nullable variant of <see cref="Core.Serialization.EnumMemberBsonSerializer{TEnum}"/> (MongoDB's NullableSerializer is sealed).
/// </summary>
public sealed class NullableEnumMemberBsonSerializer<TEnum> : SerializerBase<TEnum?> where TEnum : struct, Enum
{
	#region Fields
	
	private readonly Core.Serialization.EnumMemberBsonSerializer<TEnum> _serializer = new();
	
	#endregion
	
	#region Methods
	
	public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum? value)
	{
		if (value == null)
		{
			context.Writer.WriteNull();
			return;
		}
		
		this._serializer.Serialize(context, args, value.Value);
	}
	
	public override TEnum? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
	{
		if (context.Reader.GetCurrentBsonType() == BsonType.Null)
		{
			context.Reader.ReadNull();
			return null;
		}
		
		return this._serializer.Deserialize(context, args);
	}
	
	#endregion
}