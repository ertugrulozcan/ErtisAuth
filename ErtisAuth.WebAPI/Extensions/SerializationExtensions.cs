using System.Text.Json.Serialization;
using Ertis.Schema.Serialization;
using Ertis.MongoDB.Serialization;
using ErtisAuth.Extensions.Mailing.Serialization;

namespace ErtisAuth.WebAPI.Extensions;

public static class SerializationExtensions
{
	#region Methods
	
	public static void AddJsonSerialization(this IMvcBuilder services)
	{
		services.AddJsonOptions(options =>
		{
			options.JsonSerializerOptions.Converters.Add(new ObjectIdConverter());
			options.JsonSerializerOptions.Converters.Add(new DynamicObjectJsonConverter());
			options.JsonSerializerOptions.Converters.Add(new FieldInfoJsonConverter());
			options.JsonSerializerOptions.Converters.Add(new MailProviderJsonConverter());
			options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
		});
	}
	
	#endregion
}