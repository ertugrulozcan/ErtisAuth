using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Webhooks
{
	public class WebhookRequestDto
	{
		#region Properties

		[BsonElement("method")]
		public string Method { get; set; }
		
		[BsonElement("url")]
		public string Url { get; set; }
		
		[BsonElement("headers")]
		public Dictionary<string, object> Headers { get; set; }

		[BsonElement("body")]
		public BsonDocument Body { get; set; }
		
		#endregion
	}
}