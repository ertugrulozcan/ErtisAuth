using System.Text.Json;
using Ertis.Core.Models.Response;

namespace ErtisAuth.Sdk.Helpers;

public static class ResponseHelper
{
	#region Methods
	
	public static bool TryParseError(string json, out ErrorModel? error)
	{
		try
		{
			error = JsonSerializer.Deserialize<ErrorModel>(json);
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