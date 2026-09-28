using System.Security.Cryptography;
using System.Text;
using Ertis.Schema.Dynamics;
using Ertis.Schema.Types;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Setup;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

/// <summary>
/// The one-time setup of a fresh installation. Nothing exists yet to authenticate against, so the setup is authorized by
/// a token the operator inserts into the "setup" collection (database access proves the authority). The setup is only
/// possible while no membership exists; on success the collection is dropped.
/// </summary>
public class SetupService : ISetupService
{
	#region Constants
	
	private const string SetupTokenHeader = "X-Setup-Token";
	
	#endregion
	
	#region Services
	
	private readonly ISetupTokenRepository _setupTokenRepository;
	private readonly IMembershipService _membershipService;
	private readonly IRoleService _roleService;
	private readonly IUserService _userService;
	private readonly IUserTypeService _userTypeService;
	private readonly IApplicationService _applicationService;
	private readonly ILogger<SetupService> _logger;
	
	#endregion
	
	#region Fields
	
	/// <summary>
	/// Concurrent setup requests on this instance; the service is a singleton.
	/// </summary>
	private readonly SemaphoreSlim _setupLock = new(1, 1);
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="setupTokenRepository"></param>
	/// <param name="membershipService"></param>
	/// <param name="roleService"></param>
	/// <param name="userService"></param>
	/// <param name="userTypeService"></param>
	/// <param name="applicationService"></param>
	/// <param name="logger"></param>
	public SetupService(
		ISetupTokenRepository setupTokenRepository,
		IMembershipService membershipService,
		IRoleService roleService,
		IUserService userService,
		IUserTypeService userTypeService,
		IApplicationService applicationService,
		ILogger<SetupService> logger)
	{
		this._setupTokenRepository = setupTokenRepository;
		this._membershipService = membershipService;
		this._roleService = roleService;
		this._userService = userService;
		this._userTypeService = userTypeService;
		this._applicationService = applicationService;
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	public async Task<bool> IsSetUpAsync(CancellationToken cancellationToken = default)
	{
		var memberships = await this._membershipService.GetAsync(0, 1, cancellationToken: cancellationToken);
		return memberships.Items.Any();
	}
	
	public async Task<SetupResult> SetupAsync(string? setupToken, Membership membership, UserWithPassword user, Application? application, CancellationToken cancellationToken = default)
	{
		if (!await this._setupLock.WaitAsync(TimeSpan.Zero, cancellationToken))
		{
			throw ErtisAuthException.SetupInProgress();
		}
		
		try
		{
			if (await this.IsSetUpAsync(cancellationToken: cancellationToken))
			{
				throw ErtisAuthException.AlreadySetUp();
			}
			
			await this.VerifySetupTokenAsync(setupToken, cancellationToken: cancellationToken);
			var result = await this.CreateResourcesAsync(membership, user, application, cancellationToken: cancellationToken);
			
			// Only on success: after a failure the operator can fix the request and retry with the same token
			await this._setupTokenRepository.DropAsync(cancellationToken: cancellationToken);
			return result;
		}
		finally
		{
			this._setupLock.Release();
		}
	}
	
	private async Task VerifySetupTokenAsync(string? setupToken, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(setupToken))
		{
			throw ErtisAuthException.SetupRejected($"The {SetupTokenHeader} header is required");
		}
		
		var storedTokens = (await this._setupTokenRepository.FindAsync(x => x.Token != null, sorting: null, cancellationToken: cancellationToken)).Items
			.Select(x => x.Token!)
			.ToArray();
		
		if (storedTokens.Length == 0)
		{
			throw ErtisAuthException.SetupRejected($"No setup token found. Insert one into the '{SetupTokenRepository.CollectionNameValue}' collection, e.g. db.{SetupTokenRepository.CollectionNameValue}.insertOne({{ token: \"<at least {SetupToken.MinimumLength} characters>\" }})");
		}
		
		// A weak token would be guessable while the setup is pending
		var validTokens = storedTokens.Where(x => x.Length >= SetupToken.MinimumLength).ToArray();
		if (validTokens.Length == 0)
		{
			throw ErtisAuthException.SetupRejected($"The setup token must be at least {SetupToken.MinimumLength} characters long");
		}
		
		var actual = Encoding.UTF8.GetBytes(setupToken);
		if (!validTokens.Any(x => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(x), actual)))
		{
			throw ErtisAuthException.SetupRejected("The setup token is not valid");
		}
	}
	
	/// <summary>
	/// MongoDB transactions need a replica set, so a failed step is compensated: the resources created before it are
	/// deleted, otherwise the installation would count as set up without a usable administrator.
	/// </summary>
	private async Task<SetupResult> CreateResourcesAsync(Membership membershipModel, UserWithPassword userModel, Application? applicationModel, CancellationToken cancellationToken = default)
	{
		Membership? membership = null;
		Role? role = null;
		UserType? userType = null;
		string? userId = null;
		
		try
		{
			// 1. Membership
			membership = await this._membershipService.CreateAsync(new Membership
			{
				Name = membershipModel.Name,
				Slug = membershipModel.Slug,
				DefaultEncoding = membershipModel.DefaultEncoding,
				HashAlgorithm = membershipModel.HashAlgorithm,
				ExpiresIn = membershipModel.ExpiresIn,
				RefreshTokenExpiresIn = membershipModel.RefreshTokenExpiresIn,
				SecretKey = string.IsNullOrEmpty(membershipModel.SecretKey) ? GenerateRandomSecretKey(32) : membershipModel.SecretKey
			}, cancellationToken: cancellationToken);
			
			var utilizer = Utilizer.GetSystemUtilizer(membership.Id);
			
			// 2. Administrator role
			role = await this._roleService.EnsureAdministratorRoleAsync(membership, cancellationToken: cancellationToken);
			
			// 3. User type
			userType = await this._userTypeService.CreateAsync(utilizer, membership.Id, new UserType
			{
				Name = string.IsNullOrEmpty(userModel.UserType) ? "User" : userModel.UserType,
				Properties = Array.Empty<IFieldInfo>(),
				IsAbstract = false,
				AllowAdditionalProperties = false,
				BaseUserType = UserType.ORIGIN_USER_TYPE_SLUG,
				MembershipId = membership.Id
			}, cancellationToken: cancellationToken);
			
			// 4. Administrator user
			var user = await this._userService.CreateAsync(utilizer, membership.Id, new UserWithPassword
			{
				Username = userModel.Username,
				FirstName = userModel.FirstName,
				LastName = userModel.LastName,
				EmailAddress = userModel.EmailAddress,
				Role = role.Slug,
				UserType = userType.Slug,
				MembershipId = membership.Id,
				Password = userModel.Password,
				IsActive = true
			}, cancellationToken: cancellationToken);
			
			userId = user.TryGetValue<string>("_id", out var id, out _) ? id : null;
			
			// 5. Application
			ApplicationWithSecret? application = null;
			if (applicationModel != null)
			{
				application = await this._applicationService.CreateWithSecretAsync(utilizer, membership.Id, new Application
				{
					Name = applicationModel.Name,
					Slug = applicationModel.Slug,
					Role = applicationModel.Role,
					MembershipId = membership.Id
				}, cancellationToken: cancellationToken);
			}
			
			return new SetupResult
			{
				Membership = membership,
				User = user,
				Role = role,
				Application = application
			};
		}
		catch
		{
			await this.RollbackAsync(membership, role, userType, userId);
			throw;
		}
	}
	
	/// <summary>
	/// In reverse order of creation: a membership can't be deleted while it has resources.
	/// </summary>
	private async Task RollbackAsync(Membership? membership, Role? role, UserType? userType, string? userId)
	{
		if (membership == null)
		{
			return;
		}
		
		var utilizer = Utilizer.GetSystemUtilizer(membership.Id);
		var steps = new List<(string Resource, Func<Task> Delete)>();
		if (userId != null)
		{
			steps.Add(("user", () => this._userService.DeleteAsync(utilizer, membership.Id, userId)));
		}
		
		if (userType != null)
		{
			steps.Add(("user type", () => this._userTypeService.DeleteAsync(utilizer, membership.Id, userType.Id)));
		}
		
		if (role != null)
		{
			steps.Add(("role", () => this._roleService.DeleteAsync(utilizer, membership.Id, role.Id)));
		}
		
		steps.Add(("membership", () => this._membershipService.DeleteAsync(membership.Id)));
		
		foreach (var (resource, delete) in steps)
		{
			try
			{
				await delete();
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "Setup rollback: the {Resource} of membership {MembershipId} could not be deleted", resource, membership.Id);
			}
		}
	}
	
	private static string GenerateRandomSecretKey(int outputSize)
	{
		var bytes = RandomNumberGenerator.GetBytes(outputSize);
		return new string(bytes.Select(x => (char) (x % 26 + 65)).ToArray());
	}
	
	#endregion
}