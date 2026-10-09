using System.Net;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Setup;

/// <summary>
/// A membership secret key given to the setup must be long enough to sign the tokens (HMAC-SHA256: at least 32 bytes):
/// with a shorter key the setup is rejected and nothing is set up, so it can be run again with a valid key.
/// </summary>
public class SetupSecretKeyTests : IClassFixture<FreshErtisAuthInstance>
{
	#region Fields
	
	private readonly FreshErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public SetupSecretKeyTests(FreshErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task Setup_WithShortSecretKey_IsRejectedAndCanBeRunAgain()
	{
		await this._instance.InsertSetupTokenAsync();
		var request = new
		{
			membership = new
			{
				name = "ErtisAuth Integration",
				secret_key = "too-short-secret",
				expires_in = 3600,
				refresh_token_expires_in = 86400,
				hash_algorithm = "ARGON2ID",
				encoding = "UTF-8"
			},
			user = new
			{
				username = ErtisAuthInstance.AdminUsername,
				firstname = "Admin",
				lastname = "User",
				email_address = ErtisAuthInstance.AdminEmailAddress,
				password = ErtisAuthInstance.AdminPassword,
				user_type = "User"
			},
			application = new
			{
				name = "Server",
				role = "admin"
			}
		};
		
		using (var response = await this._instance.PostSetupAsync(body: request))
		{
			var error = await ErtisAuthInstance.ReadJsonAsync(response);
			Assert.True(response.StatusCode == HttpStatusCode.BadRequest, error.ToString());
			Assert.Contains("Secret key must be at least 32 bytes", error.ToString());
		}
		
		// Nothing was set up: the setup succeeds with the default request (a generated secret key)
		using (var response = await this._instance.PostSetupAsync())
		{
			Assert.True(response.IsSuccessStatusCode, (await ErtisAuthInstance.ReadJsonAsync(response)).ToString());
		}
	}
	
	#endregion
}
