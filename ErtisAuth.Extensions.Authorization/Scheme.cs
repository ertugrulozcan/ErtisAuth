namespace ErtisAuth.Extensions.Authorization;

public static class Scheme
{
	#region Constants
	
	public const string Name = "ErtisAuth";
	
	/// <summary>
	/// The WWW-Authenticate challenge of 401 responses (RFC 9110 §15.5.2, RFC 6750 §3). Basic is not announced:
	/// browsers answer a Basic challenge with their own login dialog, also for requests of web applications.
	/// </summary>
	public const string WwwAuthenticate = "Bearer realm=\"ErtisAuth\"";
	
	#endregion
}