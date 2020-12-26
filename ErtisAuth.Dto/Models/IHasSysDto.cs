using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models
{
	public interface IHasSysDto
	{
		[BsonIgnoreIfNull]
		[BsonElement("sys")]
		SysModelDto Sys { get; set; }
	}
}