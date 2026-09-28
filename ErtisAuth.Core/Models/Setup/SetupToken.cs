using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models.Setup;

/// <summary>
/// The secret authorizing the one-time setup of a fresh installation. The operator inserts it into the "setup"
/// collection directly (database access proves the authority); the collection is dropped when the setup completes.
/// </summary>
[BsonIgnoreExtraElements]
public class SetupToken : ResourceBase
{
	#region Constants
	
	public const int MinimumLength = 32;
	
	#endregion
	
	#region Properties
	
	[BsonElement("token")]
	public string? Token { get; set; }
	
	#endregion
}