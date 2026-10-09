using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models.Providers;

public class AppleProvider : BaseAppleProvider
{
	#region Properties
	
	[JsonIgnore]
	[BsonIgnore]
	public override ProviderType Type => ProviderType.Apple;
	
	#endregion
}