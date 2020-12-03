using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Tokens
{
	public class GenerateTokenFormModel
	{
		#region Properties

		[JsonProperty("username")]
		[Required]
		public string Username { get; set; }
		
		[JsonProperty("password")]
		[Required]
		public string Password { get; set; }

		#endregion
	}
}