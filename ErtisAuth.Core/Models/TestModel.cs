using System;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models
{
	public class TestModel
	{
		#region Properties

		[JsonProperty("_id")]
		public string Id { get; set; }
		
		[JsonProperty("string_field")]
		public string Text { get; set; }
		
		[JsonProperty("int_field")]
		public int Integer { get; set; }
		
		[JsonProperty("double_field")]
		public double Double { get; set; }
		
		[JsonProperty("datetime_field")]
		public DateTime? NullableDate { get; set; }
		
		[JsonProperty("enum_field")]
		public TestEnum Enum { get; set; }
		
		[JsonProperty("array_field")]
		public TestModel[] Array { get; set; }
		
		#endregion
	}
	
	public enum TestEnum
	{
		EnumValue1,
		EnumValue2,
		EnumValue3,
		EnumValue4,
	}
}