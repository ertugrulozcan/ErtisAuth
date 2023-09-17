using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Mailing;

public class RecipientDto
{
	#region Properties

	[BsonElement("displayName")]
	public string DisplayName { get; set; }
	
	[BsonElement("emailAddress")]
	public string EmailAddress { get; set; }

	#endregion
}