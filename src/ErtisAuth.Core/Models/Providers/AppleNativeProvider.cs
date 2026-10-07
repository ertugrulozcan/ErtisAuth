using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models.Providers;

public class AppleNativeProvider : BaseAppleProvider
{
	#region Properties
	
	[JsonIgnore]
	[BsonIgnore]
	public override ProviderType Type => ProviderType.AppleNative;
	
	#endregion
}