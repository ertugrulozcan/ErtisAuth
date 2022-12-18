namespace ErtisAuth.Integrations.OAuth.Core
{
	public interface IProviderToken
	{
		#region Properties

		string AccessToken { get; set; }
		
		long ExpiresIn { get; set; }

		#endregion
	}
}