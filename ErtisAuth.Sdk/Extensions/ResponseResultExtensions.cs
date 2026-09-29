using Ertis.Core.Models.Response;

namespace ErtisAuth.Sdk.Extensions;

public static class ResponseResultExtensions
{
	#region Methods
	
	/// <summary>
	/// The request failed because ErtisAuth could not answer it (no response, e.g. a network error, or a 5xx response),
	/// not because ErtisAuth rejected it (e.g. 401/403 for an invalid token or a missing permission).
	/// </summary>
	public static bool IsServiceUnavailable(this IResponseResult response)
	{
		return !response.IsSuccess && (response.StatusCode == null || (int) response.StatusCode >= 500);
	}
	
	#endregion
}