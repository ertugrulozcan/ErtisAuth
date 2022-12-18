namespace ErtisAuth.Integrations.OAuth.Core
{
	public interface IProviderUser
	{
		#region Properties
		
		string Id { get; set; }
		
		string FirstName { get; set; }
		
		string LastName { get; set; }
		
		string EmailAddress { get; set; }
		
		#endregion
	}
}