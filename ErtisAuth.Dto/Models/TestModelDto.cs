using System;
using System.Collections.Generic;
using Ertis.Data.Models;
using ErtisAuth.Core.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models
{
	public class TestModelDto : IEntity<string>
	{
		#region Properties

		[BsonId]
		[BsonIgnoreIfDefault]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }
		
		[BsonElement("string_field")]
		public string Text { get; set; }
		
		[BsonElement("int_field")]
		public int Integer { get; set; }
		
		[BsonElement("double_field")]
		public double Double { get; set; }
		
		[BsonElement("datetime_field")]
		public DateTime? NullableDate { get; set; }
		
		[BsonElement("enum_field")]
		public TestEnum Enum { get; set; }
		
		[BsonElement("array_field")]
		public TestModelDto[] Array { get; set; }
		
		#endregion

		#region Methods

		public static IEnumerable<TestModelDto> GenerateRandom(int count)
		{
			List<TestModelDto> list = new List<TestModelDto>();
			for (int i = 0; i < count; i++)
			{
				list.Add(GenerateRandom(i + 1, 2));
			}

			return list;
		}
		
		public static TestModelDto GenerateRandom(int no, int childCount)
		{
			var children = new List<TestModelDto>();
			for (int i = 0; i < childCount; i++)
			{
				children.Add(GenerateRandom(no + 10, 0));
			}
			
			var random = new Random((int)DateTime.Now.Ticks);
			return new TestModelDto
			{
				Text = $"Entity - {no}",
				Integer = no,
				Double = no * random.NextDouble(),
				NullableDate = DateTime.Now.AddDays(no),
				Enum = (TestEnum) (no % 4),
				Array = children.ToArray()
			};
		}

		#endregion
	}
}