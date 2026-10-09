using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models.Identity;

/// <summary>
/// The response of a token code creation: the only place the plain device code is returned.
/// Never pass this model to events or caches, the device code would leak with it.
/// </summary>
public class TokenCodeWithDeviceCode : TokenCode
{
	#region Properties
	
	[JsonPropertyName("device_code")]
	[BsonIgnore]
	public required string DeviceCode { get; init; }
	
	#endregion
}
