using System.Threading.Tasks;
using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Dao.Repositories;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Identity.Jwt.Services;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using ErtisAuth.Infrastructure.Adapters;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Tests.MockServices;
using NUnit.Framework;

namespace ErtisAuth.Tests.BusinessTests
{
	public class TokenTests
	{
		#region Services

		private ITokenService tokenService;

		#endregion
		
		#region Setup

		[SetUp]
		public void Setup()
		{
			IDatabaseSettings databaseSettings = new DatabaseSettings
			{
				Host = "172.17.0.2",
				Port = 27017,
				DefaultAuthDatabase = "ertisauth"
			};

			IScopeOwnerAccessor scopeOwnerAccessor = new MockScopeOwnerAccessor();
			IRepositoryActionBinder repositoryActionBinder = new SysUpserter(scopeOwnerAccessor);
			IMembershipRepository membershipRepository = new MembershipRepository(databaseSettings, repositoryActionBinder);
			IMembershipService membershipService = new MembershipService(membershipRepository);
			IEventRepository eventRepository = new EventRepository(databaseSettings, repositoryActionBinder);
			IEventService eventService = new EventService(membershipService, eventRepository);
			IRoleRepository roleRepository = new RoleRepository(databaseSettings, repositoryActionBinder);
			IRoleService roleService = new RoleService(membershipService, eventService, roleRepository);
			IJwtService jwtService = new JwtService();
			ICryptographyService cryptographyService = new CryptographyService();
			IUserRepository userRepository = new UserRepository(databaseSettings, repositoryActionBinder);
			IUserService userService = new UserService(membershipService, roleService, eventService, jwtService, cryptographyService, userRepository);
			IApplicationRepository applicationRepository = new ApplicationRepository(databaseSettings, repositoryActionBinder);
			IApplicationService applicationService = new ApplicationService(membershipService, roleService, eventService, applicationRepository);
			IRevokedTokensRepository revokedTokensRepository = new RevokedTokensRepository(databaseSettings);
			
			this.tokenService = new TokenService(
				membershipService,
				userService,
				applicationService,
				jwtService,
				cryptographyService,
				eventService,
				revokedTokensRepository);
		}

		#endregion
		
		#region Methods

		/*
		[Test]
		public async Task GenerateTokenShouldReturnOk()
		{
			var token = await this.tokenService.GenerateTokenAsync(username, password, membershipId);
			if (token != null)
			{
				
			}
			else
			{
				
			}
		}
		*/

		#endregion	
	}
}