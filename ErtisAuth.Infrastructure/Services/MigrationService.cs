using System.Security.Cryptography;
using Ertis.MongoDB.Configuration;
using Ertis.Schema.Types;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Applications;

namespace ErtisAuth.Infrastructure.Services;

public class MigrationService : IMigrationService
{
	#region Services
	
	private readonly IDatabaseSettings _databaseSettings;
	private readonly IMembershipService _membershipService;
	private readonly IRoleService _roleService;
	private readonly IUserService _userService;
	private readonly IUserTypeService _userTypeService;
	private readonly IApplicationService _applicationService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="databaseSettings"></param>
	/// <param name="membershipService"></param>
	/// <param name="roleService"></param>
	/// <param name="userService"></param>
	/// <param name="userTypeService"></param>
	/// <param name="applicationService"></param>
	public MigrationService(
		IDatabaseSettings databaseSettings,
		IMembershipService membershipService,
		IRoleService roleService,
		IUserService userService,
		IUserTypeService userTypeService,
		IApplicationService applicationService)
	{
		this._databaseSettings = databaseSettings;
		this._membershipService = membershipService;
		this._roleService = roleService;
		this._userService = userService;
		this._userTypeService = userTypeService;
		this._applicationService = applicationService;
	}
	
	#endregion
	
	#region Methods
	
	public async ValueTask<dynamic> MigrateAsync(string connectionString, Membership _membership, UserWithPassword _user, Application? _application)
	{
		// Validation
		if (connectionString != this._databaseSettings.ConnectionString)
		{
			throw ErtisAuthException.MigrationRejected("Connection string could not validated");
		}
		
		// 1. Membership
		var membership = await this._membershipService.CreateAsync(new Membership
		{
			Name = _membership.Name,
			Slug = _membership.Slug,
			DefaultEncoding = _membership.DefaultEncoding,
			HashAlgorithm = _membership.HashAlgorithm,
			ExpiresIn = _membership.ExpiresIn,
			RefreshTokenExpiresIn = _membership.RefreshTokenExpiresIn,
			SecretKey = string.IsNullOrEmpty(_membership.SecretKey) ? GenerateRandomSecretKey(32) : _membership.SecretKey
		});
		
		// Utilizer
		var utilizer = Utilizer.GetSystemUtilizer(membership.Id);
		
		// 2. Role
		var adminRole = await this._roleService.GetBySlugAsync(ReservedRoles.Administrator, membership.Id);
		if (adminRole == null)
		{
			throw ErtisAuthException.RoleNotFound("admin");
		}
		
		// 3. User Type
		var userType = await this._userTypeService.CreateAsync(utilizer, membership.Id, new UserType
		{
			Name = string.IsNullOrEmpty(_user.UserType) ? "User" : _user.UserType,
			Properties = Array.Empty<IFieldInfo>(),
			IsAbstract = false,
			AllowAdditionalProperties = false,
			BaseUserType = UserType.ORIGIN_USER_TYPE_SLUG,
			MembershipId = membership.Id
		});
		
		// 4. User (admin)
		var adminUser = await this._userService.CreateAsync(utilizer, membership.Id, new UserWithPassword
		{
			Username = _user.Username,
			FirstName = _user.FirstName,
			LastName = _user.LastName,
			EmailAddress = _user.EmailAddress,
			Role = adminRole.Slug,
			UserType = userType.Slug,
			MembershipId = membership.Id,
			Password = _user.Password,
			IsActive = true
		});
		
		// 5. Application
		if (_application != null)
		{
			var application = await this._applicationService.CreateAsync(utilizer, membership.Id, new Application
			{
				Name = _application.Name,
				Slug = _application.Slug,
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
		var bytes = new byte[outputSize];
		randomNumberGenerator.GetBytes(bytes);
		return new string(bytes.Select(x => (char) (x % 26 + 65)).ToArray());
	}
	
	#endregion
}