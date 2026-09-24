using System.Text.Json.Serialization;
using Ertis.Core.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models;

public interface IHasSysInfo
{
	#region Properties
	
	[JsonProperty("sys")]
	[JsonPropertyName("sys")]
	[BsonElement("sys")]
	SysModel? Sys { get; set; }
	
	#endregion
}