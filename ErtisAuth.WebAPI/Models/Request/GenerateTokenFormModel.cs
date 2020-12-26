using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request
{
	public class GenerateTokenFormModel
	{
		#region Properties

		[JsonProperty("username")]
		[Required(ErrorMessage = "'username' is a required field")]
		public string Username { get; set; }
		
		[JsonProperty("password")]
		[Required(ErrorMessage = "'password' is a required field")]
		public string Password { get; set; }

		#endregion
	}
}