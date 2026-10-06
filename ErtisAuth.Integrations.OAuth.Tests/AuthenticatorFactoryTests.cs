using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Tests.Helpers;

namespace ErtisAuth.Integrations.OAuth.Tests;

/// <summary>
/// Every provider type gets an authenticator that accepts it: the typed authenticators cast the provider and a
/// provider of another type fails the cast (ArgumentException, 500 on login).
/// </summary>
public class AuthenticatorFactoryTests
{
	#region Fields
	
	private readonly OAuthTestServices _services = new();
	
	#endregion
	
	#region Helpers
	
	private static Provider CreateProvider(string type)
	{
		const string membershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
		return type switch
		{
			nameof(AppleProvider) => new AppleProvider { MembershipId = membershipId },
			nameof(AppleNativeProvider) => new AppleNativeProvider { MembershipId = membershipId },
			nameof(FacebookProvider) => new FacebookProvider { MembershipId = membershipId },
			nameof(GoogleProvider) => new GoogleProvider { MembershipId = membershipId },
			nameof(MicrosoftProvider) => new MicrosoftProvider { MembershipId = membershipId },
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}
	
	#endregion
	
	#region Tests
	
	[Theory]
	[InlineData(nameof(AppleProvider))]
	[InlineData(nameof(AppleNativeProvider))]
	[InlineData(nameof(FacebookProvider))]
	[InlineData(nameof(GoogleProvider))]
	[InlineData(nameof(MicrosoftProvider))]
	public void GetAuthenticator_ReturnsAnAuthenticatorOfTheProvidersType(string type)
	{
		var provider = CreateProvider(type);
		
		var authenticator = this._services.Get<IAuthenticatorFactory>().GetAuthenticator(provider);
		
		var typedInterface = Assert.Single(authenticator.GetType().GetInterfaces(), x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IProviderAuthenticator<,,,>));
		Assert.IsAssignableFrom(typedInterface.GetGenericArguments()[0], provider);
	}
	
	#endregion
}
