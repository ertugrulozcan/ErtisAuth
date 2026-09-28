using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.IntegrationTests.Otp;

/// <summary>
/// The OTP attempt limit on a real MongoDB: attempts are reserved atomically (FindOneAndUpdate with the limit in the
/// filter), so parallel guesses can't exceed max_attempts. The unit tests only mock this filter.
/// </summary>
public class OtpAttemptTests : IClassFixture<ErtisAuthInstance>
{
	#region Constants
	
	private const string OtpHost = "https://app.example.com";
	private const int MaxAttempts = 5;
	
	#endregion
	
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	public OtpAttemptTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private IOneTimePasswordRepository Repository => this._instance.Factory.Services.GetRequiredService<IOneTimePasswordRepository>();
	
	private async Task<OneTimePassword> InsertOtpAsync()
	{
		return await this.Repository.InsertAsync(new OneTimePassword
		{
			MembershipId = this._instance.MembershipId,
			UserId = "5f8a1b2c3d4e5f6a7b8c9d01",
			Username = $"otp-user-{Guid.NewGuid():N}",
			EmailAddress = "otp.user@example.com",
			PasswordHash = "hash",
			Token = new ResetPasswordToken("reset-token", TimeSpan.FromMinutes(5))
		}, cancellationToken: TestContext.Current.CancellationToken);
	}
	
	private async Task EnableOtpAsync()
	{
		await this._instance.UpdateMembershipAsync(membership => membership["otp_settings"] = new JsonObject
		{
			["host"] = OtpHost,
			["policy"] = new JsonObject
			{
				["length"] = 6,
				["contains_digits"] = true,
				["contains_letters"] = false,
				["expires_in"] = 300,
				["max_attempts"] = MaxAttempts
			}
		});
	}
	
	private async Task<HttpResponseMessage> VerifyOtpAsync(string code)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "/verify-otp")
		{
			Content = JsonContent.Create(new { username = ErtisAuthInstance.AdminUsername, password = code })
		};
		
		request.Headers.Add("Membership", this._instance.MembershipId);
		request.Headers.Add("X-Host", OtpHost);
		return await this._instance.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Repository
	
	[Fact]
	public async Task TryReserveAttemptAsync_InParallel_ReservesExactlyMaxAttempts()
	{
		var otp = await this.InsertOtpAsync();
		
		var reservations = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => this.Repository.TryReserveAttemptAsync(otp.Id, MaxAttempts, TestContext.Current.CancellationToken)));
		
		Assert.Equal(MaxAttempts, reservations.Count(x => x != null));
		Assert.Equal(Enumerable.Range(1, MaxAttempts), reservations.Where(x => x != null).Select(x => x!.FailedAttempts).Order());
		Assert.Equal(MaxAttempts, (await this.Repository.FindOneAsync(otp.Id, TestContext.Current.CancellationToken))!.FailedAttempts);
	}
	
	[Fact]
	public async Task ReleaseAttemptAsync_GivesAnAttemptBackButNotBelowZero()
	{
		var otp = await this.InsertOtpAsync();
		await this.Repository.TryReserveAttemptAsync(otp.Id, MaxAttempts, TestContext.Current.CancellationToken);
		
		await this.Repository.ReleaseAttemptAsync(otp.Id, TestContext.Current.CancellationToken);
		await this.Repository.ReleaseAttemptAsync(otp.Id, TestContext.Current.CancellationToken);
		
		Assert.Equal(0, (await this.Repository.FindOneAsync(otp.Id, TestContext.Current.CancellationToken))!.FailedAttempts);
	}
	
	#endregion
	
	#region HTTP
	
	[Fact]
	public async Task VerifyOtp_AfterParallelWrongGuesses_RejectsTheCorrectCode()
	{
		await this.EnableOtpAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		var adminUserId = await this._instance.GetAdminUserIdAsync();
		var otp = await adminClient.GetFromJsonAsync<JsonObject>($"/memberships/{this._instance.MembershipId}/users/{adminUserId}/generate-otp", TestContext.Current.CancellationToken);
		var code = otp!["password"]!.GetValue<string>();
		var wrongCode = code == "999999" ? "888888" : "999999";
		
		// A correct verification works and doesn't use up an attempt
		using (var response = await this.VerifyOtpAsync(code))
		{
			Assert.True(response.IsSuccessStatusCode, (await ErtisAuthInstance.ReadJsonAsync(response)).ToString());
		}
		
		// Attack: many parallel guesses
		var guesses = await Task.WhenAll(Enumerable.Range(0, 3 * MaxAttempts).Select(_ => this.VerifyOtpAsync(wrongCode)));
		Assert.All(guesses, x => Assert.Equal(HttpStatusCode.Unauthorized, x.StatusCode));
		
		// The attempts are used up: the one-time password is gone, even the correct code is rejected
		using var afterAttack = await this.VerifyOtpAsync(code);
		Assert.Equal(HttpStatusCode.Unauthorized, afterAttack.StatusCode);
	}
	
	#endregion
}