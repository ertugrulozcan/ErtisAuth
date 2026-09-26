using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

internal static class TestServiceFactory
{
	#region Methods
	
	public static UserService CreateUserService(IMembershipService? membershipService = null, IUserRepository? repository = null, IEventService? eventService = null)
	{
		return new UserService(
			Substitute.For<IUserTypeService>(),
			membershipService ?? Substitute.For<IMembershipService>(),
			Substitute.For<IRoleService>(),
			Substitute.For<IAccessControlService>(),
			eventService ?? Substitute.For<IEventService>(),
			Substitute.For<IJwtService>(),
			Substitute.For<IMailHookService>(),
			repository ?? Substitute.For<IUserRepository>(),
			NullLogger<UserService>.Instance);
	}
	
	public static Membership CreateMembership(string? hashAlgorithm = null, string? defaultEncoding = null)
	{
		return new Membership
		{
			Id = "membership-id",
			Name = "Test Membership",
			SecretKey = "test-secret-key-test-secret-key-test-secret-key",
			ExpiresIn = 3600,
			RefreshTokenExpiresIn = 7200,
			HashAlgorithm = hashAlgorithm,
			DefaultEncoding = defaultEncoding
		};
	}
	
	#endregion
}
