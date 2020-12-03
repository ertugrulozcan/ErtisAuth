using Ertis.Data.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models
{
	public abstract class EntityBase : IEntity<string>
	{
		#region Properties

		[BsonId]
		[BsonIgnoreIfDefault]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }

		#endregion
	}
}