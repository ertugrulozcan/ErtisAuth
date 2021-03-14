using Ertis.Net.Rest;
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

		#endregion
	}
}