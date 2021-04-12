using System;
using System.Globalization;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services;
using ErtisAuth.Sdk.Services.Interfaces;
using NUnit.Framework;

namespace ErtisAuth.Tests.Sdk.ErtisAuth.Sdk.Tests
{
	public class TokenTests
	{
		#region Services

		private IAuthenticationService authenticationService;

		#endregion
		
		#region Setup

		[SetUp]
		public void Setup()
		{
			var ertisAuthOptions = new ErtisAuthOptions
			{
				BaseUrl = "http://localhost:9716/api/v1",
				MembershipId = "604b9f173169a6d98b045a56",
				ApplicationId = "604b9f1a3169a6d98b045a5b",
				Secret = "UKHQBFRTWLTVQBPIPYREWVOZKEASBZQI"
			};
			
			this.authenticationService = new AuthenticationService(ertisAuthOptions, new RestSharpRestHandler());
		}

		#endregion
		
		#region Methods

		[Test]
		public void GenerateTokenTest()
		{
			var getTokenResponse = this.authenticationService.GetToken("ertugrul.ozcan", ".Abcd1234!");
			if (getTokenResponse.IsSuccess)
			{
				
			}
		}

		[Test]
		public void BearerToken_ParseFromJson_Test()
		{
			string json =
				"{\"token_type\": \"bearer\", \"refresh_token\": \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\", \"refresh_token_expires_in\": 86400, \"access_token\": \"ayJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\", \"expires_in\": 3600, \"created_at\": \"2021-04-12T21:04:13.4757966+00:00\"}";

			var bearerToken = BearerToken.ParseFromJson(json);
			Assert.AreEqual(SupportedTokenTypes.Bearer, bearerToken.TokenType);
			Assert.AreEqual("ayJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", bearerToken.AccessToken);
			Assert.AreEqual("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", bearerToken.RefreshToken);
			Assert.AreEqual(TimeSpan.FromSeconds(86400), bearerToken.RefreshExpiresIn);
			Assert.AreEqual(86400, bearerToken.RefreshTokenExpiresInTimeStamp);
			Assert.AreEqual(3600, bearerToken.ExpiresInTimeStamp);
			Assert.AreEqual(DateTime.Parse("2021-04-12T21:04:13", CultureInfo.InvariantCulture).AddHours(3), bearerToken.CreatedAt);
		}

		#endregion
	}
}