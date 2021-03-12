using Ertis.Core.Models.Response;
using Newtonsoft.Json;

namespace ErtisAuth.Extensions.AspNetCore.Helpers
{
	public static class ResponseHelper
	{
		#region Methods

		public static bool TryParseError(string json, out ErrorModel error)
		{
			try
			{
				error = JsonConvert.DeserializeObject<ErrorModel>(json);
				return error != null;
			}
			catch
			{
				error = null;
				return false;
			}
		}

		#endregion
	}
}