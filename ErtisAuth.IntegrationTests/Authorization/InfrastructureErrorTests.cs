using System.Net;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;

namespace ErtisAuth.IntegrationTests.Authorization;

/// <summary>
/// An infrastructure error during authentication (the database is unreachable) is not an invalid token: the global
/// exception handler answers 500 with the generic error body, so that clients don't sign their users out (401).
/// </summary>
public class InfrastructureErrorTests : IClassFixture<OutageErtisAuthInstance>
{
	#region Fields
	
	private readonly OutageErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	public InfrastructureErrorTests(OutageErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Tests
	
	[Theory]
	[InlineData("/memberships/{membershipId}/users")]
	[InlineData("/memberships/{membershipId}/roles")]
	[InlineData("/memberships/{membershipId}/roles/check-permission?permission=users.read")]
	public async Task DatabaseOutageDuringAuthentication_IsServerError(string path)
	{
		var client = this._instance.CreateClient($"Bearer {OutageErtisAuthInstance.OutageToken}");
		
		using var response = await client.GetAsync(path.Replace("{membershipId}", this._instance.MembershipId), TestContext.Current.CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.InternalServerError);
		Assert.Equal("An unexpected error occurred", error!["message"]!.GetValue<string>());
		Assert.Equal("UnhandledExceptionError", error["errorCode"]!.GetValue<string>());
		Assert.DoesNotContain("timeout", error.ToJsonString(), StringComparison.OrdinalIgnoreCase);
	}
	
	[Fact]
	public async Task InvalidToken_IsStillUnauthorized()
	{
		var client = this._instance.CreateClient("Bearer not-a-token");
		
		using var response = await client.GetAsync("/me", TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}
	
	#endregion
}