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
		UserPasswordReset = 210,
		UserPasswordChanged = 211,
		
		UserTypeCreated = 250,
		UserTypeUpdated = 251,
		UserTypeDeleted = 252,
		
		ApplicationCreated = 300,
		ApplicationUpdated = 301,
		ApplicationDeleted = 302,
		
		RoleCreated = 400,
		RoleUpdated = 401,
		RoleDeleted = 402,
		
		ProviderCreated = 500,
		ProviderUpdated = 501,
		ProviderDeleted = 502,
		
		WebhookCreated = 600,
		WebhookUpdated = 601,
		WebhookDeleted = 602,
		WebhookRequestSent = 610,
		WebhookRequestFailed = 611,
		
		MailhookCreated = 700,
		MailhookUpdated = 701,
		MailhookDeleted = 702,
		MailhookMailSent = 710,
		MailhookMailFailed = 711,
		
		TokenCodePolicyCreated = 800,
		TokenCodePolicyUpdated = 801,
		TokenCodePolicyDeleted = 802,
	}
}