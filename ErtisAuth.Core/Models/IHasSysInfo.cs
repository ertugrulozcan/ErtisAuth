using System.Text.Json.Serialization;
using Ertis.Core.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models;

public interface IHasSysInfo
{
	#region Properties
	
	[JsonPropertyName("sys")]
	[BsonElement("sys")]
	SysModel? Sys { get; set; }
	
	#endregion
}