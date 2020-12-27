using ErtisAuth.Core.Models.UserTypes;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.UserTypes
{
	public class CreateUserTypeFormModel
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("description")]
		public string Description { get; set; }
		
		[JsonProperty("schema")]
		public SchemaProperty Schema { get; set; }
		
		#endregion
	}
}