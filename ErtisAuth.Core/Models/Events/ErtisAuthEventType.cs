namespace ErtisAuth.Core.Models.Events
{
	public enum ErtisAuthEventType
	{
		TokenGenerated,
		TokenRefreshed,
		TokenVerified,
		TokenRevoked
	}
}