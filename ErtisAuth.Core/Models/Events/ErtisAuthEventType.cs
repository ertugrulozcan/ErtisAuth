namespace ErtisAuth.Core.Models.Events
{
	public enum ErtisAuthEventType
	{
		TokenGenerated = 100,
		TokenRefreshed = 101,
		TokenVerified = 102,
		TokenRevoked = 103,
		
		UserCreated = 200,
		UserUpdated = 201,
		UserDeleted = 202,
		
		ApplicationCreated = 300,
		ApplicationUpdated = 301,
		ApplicationDeleted = 302,
		
		RoleCreated = 400,
		RoleUpdated = 401,
		RoleDeleted = 402,
	}
}