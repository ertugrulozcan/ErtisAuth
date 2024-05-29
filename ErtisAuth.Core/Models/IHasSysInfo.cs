using System.Text.Json.Serialization;
using Ertis.Core.Models.Resources;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models
{
	public interface IHasSysInfo
	{
		#region Properties
		
		[JsonProperty("sys")]
		[JsonPropertyName("sys")]
		SysModel Sys { get; set; }

		#endregion
	}
}