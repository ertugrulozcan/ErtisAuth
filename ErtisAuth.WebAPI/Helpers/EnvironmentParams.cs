using System.Collections.Generic;

namespace ErtisAuth.WebAPI.Helpers;

public static class EnvironmentParams
{
	#region Properties

	private static readonly Dictionary<string, object> EnvironmentParameters = new();

	#endregion
	
	#region Methods

	public static void SetEnvironmentParameter(string key, object value)
	{
		if (EnvironmentParameters.ContainsKey(key))
			EnvironmentParameters[key] = value;
		else
			EnvironmentParameters.Add(key, value);
	}
		
	public static object GetEnvironmentParameter(string key)
	{
		if (EnvironmentParameters.ContainsKey(key))
			return EnvironmentParameters[key];

		return null;
	}

	#endregion
}