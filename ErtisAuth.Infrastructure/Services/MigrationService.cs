using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Infrastructure.Helpers;

namespace ErtisAuth.Infrastructure.Services
{
	public class MigrationService : IMigrationService
	{
		#region Services
		
		private readonly IDatabaseSettings databaseSettings;
		private readonly IMembershipService membershipService;
		private readonly IRoleService roleService;
		private readonly IUserService userService;
		private readonly IApplicationService applicationService;
		private readonly ICryptographyService cryptographyService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="databaseSettings"></param>
		/// <param name="membershipService"></param>
		/// <param name="roleService"></param>
		/// <param name="userService"></param>
		/// <param name="applicationService"></param>
		/// <param name="cryptographyService"></param>
		public MigrationService(
			IDatabaseSettings databaseSettings,
			IMembershipService membershipService,
			IRoleService roleService,
			IUserService userService,
			IApplicationService applicationService,
			ICryptographyService cryptographyService)
		{
			this.databaseSettings = databaseSettings;
			this.membershipService = membershipService;
			this.roleService = roleService;
			this.userService = userService;
			this.applicationService = applicationService;
			this.cryptographyService = cryptographyService;
		}

		#endregion
		
		#region Methods

		public async ValueTask<dynamic> MigrateAsync(string connectionString, Membership _membership, UserWithPasswordHash _user, Application _application)
		{
			// Validation
			var databaseInformation = Ertis.MongoDB.Helpers.ConnectionStringHelper.ParseConnectionString(connectionString);
			var connectionString1 = Ertis.MongoDB.Helpers.ConnectionStringHelper.GenerateConnectionString(this.databaseSettings);
			var connectionString2 = Ertis.MongoDB.Helpers.ConnectionStringHelper.GenerateConnectionString(databaseInformation);
			
			if (connectionString1 != connectionString2)
			{
				throw ErtisAuthException.MigrationRejected("Connection string could not validated");
			}

			// 1. Membership
			var membership = await this.membershipService.CreateAsync(new Membership
			{
				Name = _membership.Name,
				DefaultEncoding = _membership.DefaultEncoding,
				HashAlgorithm = _membership.HashAlgorithm,
				ExpiresIn = _membership.ExpiresIn,
				RefreshTokenExpiresIn = _membership.RefreshTokenExpiresIn,
				SecretKey = string.IsNullOrEmpty(_membership.SecretKey) ? GenerateRandomSecretKey(32) : _membership.SecretKey
			});

			// Utilizer
			var utilizer = new Utilizer
			{
				Username = "migration",
				Role = ReservedRoles.Administrator,
				Type = Utilizer.UtilizerType.System,
				MembershipId = membership.Id
			};
			
			// 2. Role
			Role adminRole;
			var currentAdminRole = await this.roleService.GetByNameAsync(ReservedRoles.Administrator, membership.Id);
			if (currentAdminRole == null)
			{
				adminRole = await this.roleService.CreateAsync(utilizer, membership.Id, new Role
				{
					Name = ReservedRoles.Administrator,
					Description = "Administrator",
					MembershipId = membership.Id,
					Permissions = RoleHelper.AssertAdminPermissionsForReservedResources()
				});
			}
			else
			{
				adminRole = currentAdminRole;
			}

			// 3. User (admin)
			var adminUser = await this.userService.CreateAsync(utilizer, membership.Id, new UserWithPasswordHash
			{
				Username = _user.Username,
				FirstName = _user.FirstName,
				LastName = _user.LastName,
				EmailAddress = _user.EmailAddress,
				Role = adminRole.Name,
				MembershipId = membership.Id,
				PasswordHash = this.cryptographyService.CalculatePasswordHash(membership, _user.PasswordHash)
			});
			
			// 4. Application
			if (_application != null)
			{
				var application = await this.applicationService.CreateAsync(utilizer, membership.Id, new Application
				{
					Name = _application.Name,
					Role = _application.Role,
					MembershipId = membership.Id
				});

				return new
				{
					membership,
					user = adminUser,
					role = adminRole,
					application
				};	
			}
			
			return new
			{
				membership,
				user = adminUser,
				role = adminRole
			};
		}

		private static string GenerateRandomSecretKey(int outputSize)
		{
			var randomNumberGenerator = RandomNumberGenerator.Create();
			byte[] bytes = new byte[outputSize];
			randomNumberGenerator.GetBytes(bytes);
			return new string(bytes.Select(x => (char) (x % 26 + 65)).ToArray());
		}

		#endregion
	}
}