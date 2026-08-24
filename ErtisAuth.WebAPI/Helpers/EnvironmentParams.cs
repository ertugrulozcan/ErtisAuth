namespace ErtisAuth.WebAPI.Helpers;

public static class EnvironmentParams
{
	#region Properties
	
	private static readonly Dictionary<string, object> EnvironmentParameters = new();
	
	#endregion
	
	#region Methods
	
	public static void SetEnvironmentParameter(string key, object value)
	{
		EnvironmentParameters[key] = value;
	}
	
	public static object? GetEnvironmentParameter(string key)
	{
		return EnvironmentParameters.GetValueOrDefault(key);
	}
	
	#endregion
}