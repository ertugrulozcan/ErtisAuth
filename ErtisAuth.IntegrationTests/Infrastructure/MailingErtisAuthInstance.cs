using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// An installation that sends its mails to a <see cref="FakeSmtpServer"/>: the membership has an SMTP mail provider
/// and the predefined "User Activation" and "Reset Password" mail hooks, whose bodies are the action links.
/// </summary>
public class MailingErtisAuthInstance : ErtisAuthInstance
{
	#region Constants
	
	public const string MailProviderSlug = "fake-smtp";
	
	public const string Host = "https://app.example.com/account";
	
	#endregion
	
	#region Properties
	
	public FakeSmtpServer Smtp { get; } = new();
	
	/// <summary>
	/// The membership's user_activation: when active, new users are inactive until they follow the activation link.
	/// </summary>
	protected virtual string UserActivation => "passive";
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mongo"></param>
	// ReSharper disable once MemberCanBeProtected.Global
	public MailingErtisAuthInstance(MongoDbContainerFixture mongo) : base(mongo)
	{
	
	}
	
	#endregion
	
	#region Lifetime
	
	protected override async Task OnSetUpAsync()
	{
		// Mail hooks first: a membership with user activation requires the activation mail hook
		var adminClient = await this.CreateAdminClientAsync();
		await CreateMailHookAsync(adminClient, $"/memberships/{this.MembershipId}/mailhooks", "User Activation", "UserCreated", "Activate your account", "<a href=\"{{activationLink}}\">Activate</a>");
		await CreateMailHookAsync(adminClient, $"/memberships/{this.MembershipId}/mailhooks", "Reset Password", "UserPasswordReset", "Reset your password", "<a href=\"{{resetPasswordLink}}\">Reset</a>");
		
		await this.UpdateMembershipAsync(membership =>
		{
			membership["user_activation"] = this.UserActivation;
			membership["mail_providers"] = new JsonArray
			{
				new JsonObject
				{
					["type"] = "SmtpServer",
					["name"] = "Fake Smtp",
					["host"] = this.Smtp.Host,
					["port"] = this.Smtp.Port,
					["tls_enabled"] = false,
					["username"] = "smtp-user",
					["password"] = "smtp-password"
				}
			};
		});
	}
	
	private static async Task CreateMailHookAsync(HttpClient adminClient, string url, string name, string eventType, string subject, string template)
	{
		using var response = await adminClient.PostAsJsonAsync(url, new
		{
			name,
			@event = eventType,
			status = "active",
			mailProvider = MailProviderSlug,
			mailSubject = subject,
			mailTemplate = template,
			fromName = "ErtisAuth",
			fromAddress = "no-reply@example.com",
			sendToUtilizer = true
		});
		
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"Mail hook '{name}' could not be created ({(int) response.StatusCode}): {await ReadJsonAsync(response)}");
		}
	}
	
	public override async ValueTask DisposeAsync()
	{
		await base.DisposeAsync();
		await this.Smtp.DisposeAsync();
	}
	
	#endregion
}

/// <summary>
/// A <see cref="MailingErtisAuthInstance"/> with user activation: new users are inactive until activated.
/// </summary>
public sealed class ActivationErtisAuthInstance : MailingErtisAuthInstance
{
	#region Properties
	
	protected override string UserActivation => "active";
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mongo"></param>
	public ActivationErtisAuthInstance(MongoDbContainerFixture mongo) : base(mongo)
	{
	
	}
	
	#endregion
}