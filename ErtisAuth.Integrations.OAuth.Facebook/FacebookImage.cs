using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Facebook
{
	public class FacebookImage
	{
		#region Properties

		[JsonProperty("url")]
		public string Url { get; set; }
		
		[JsonProperty("width")]
		public int Width { get; set; }
		
		[JsonProperty("height")]
		public int Height { get; set; }
		
		[JsonProperty("is_silhouette")]
		public bool IsSilhouette { get; set; }
		
		#endregion
	}
}