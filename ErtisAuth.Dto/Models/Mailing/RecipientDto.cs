using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Dto.Models.Mailing;

public class RecipientDto
{
	#region Properties
	
	[BsonElement("displayName")]
	public string? DisplayName { get; set; }
	
	[BsonElement("emailAddress")]
	public string? EmailAddress { get; set; }
	
	#endregion
}