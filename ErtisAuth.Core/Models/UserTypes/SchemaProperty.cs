using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Core.Models.UserTypes
{
	public class SchemaProperty
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonConverter(typeof(StringEnumConverter))]
		[JsonProperty("type")]
		public SchemaPropertyType Type { get; set; }

		[JsonProperty("default_value")]
		public object DefaultValue { get; set; }
		
		[JsonProperty("is_required")]
		public bool IsRequired { get; set; }
		
		[JsonProperty("properties")]
		public IEnumerable<SchemaProperty> Properties { get; set; }
		
		#endregion
	}

	public enum SchemaPropertyType
	{
		Object,
		String,
		Integer,
		Double,
		Boolean,
		DateTime
	}
}