using ErtisAuth.Core.Models.Applications;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

/// <summary>
/// The stored secret hash must never reach clients, events or webhooks; the plain secret only appears in ApplicationWithSecret.
/// </summary>
public class ApplicationSerializationTests
{
	#region Helpers
	
	private static Application CreateApplication()
	{
		return new Application
		{
			Id = "6a7b8c9d0e1f2a3b4c5d6e7f",
			Name = "Server App",
			Role = "server",
			MembershipId = "membership-id",
			SecretHash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"
		};
	}
	
	private static IEnumerable<string> Serialize(object value)
	{
		yield return System.Text.Json.JsonSerializer.Serialize(value, value.GetType());
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public void Application_IsSerializedWithoutSecretHash()
	{
		foreach (var json in Serialize(CreateApplication()))
		{
			Assert.DoesNotContain("secret_hash", json);
			Assert.DoesNotContain("SecretHash", json);
			Assert.DoesNotContain("ba7816bf8f01cfea", json);
		}
	}
	
	[Fact]
	public void ApplicationWithSecret_IsSerializedWithSecretButWithoutSecretHash()
	{
		var application = new ApplicationWithSecret(CreateApplication(), "plain-secret");
		foreach (var json in Serialize(application))
		{
			Assert.Contains("\"secret\":\"plain-secret\"", json);
			Assert.Contains("\"name\":\"Server App\"", json);
			Assert.DoesNotContain("secret_hash", json);
			Assert.DoesNotContain("ba7816bf8f01cfea", json);
		}
	}
	
	#endregion
}
