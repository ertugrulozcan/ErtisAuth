using System.Security.Cryptography;
using System.Text;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Memberships;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

// LEGACY-APP-SECRET: temporary, remove this class (and its registration and usage in TokenService)
// after all applications are migrated to their own secrets
/// <summary>
/// Decides whether an application without its own secret may still authenticate with the membership secret key,
/// and logs such applications so that the remaining ones can be found and migrated.
/// </summary>
public class LegacyApplicationSecretVerifier
{
	#region Constants
	
	private const string CACHE_KEY = "legacy-application-secret-warnings";
	
	private static readonly TimeSpan WarningInterval = TimeSpan.FromHours(1);
	
	#endregion
	
	#region Services
	
	private readonly IMemoryCache _memoryCache;
	private readonly ILogger<LegacyApplicationSecretVerifier> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="memoryCache"></param>
	/// <param name="logger"></param>
	public LegacyApplicationSecretVerifier(IMemoryCache memoryCache, ILogger<LegacyApplicationSecretVerifier> logger)
	{
		this._memoryCache = memoryCache;
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Only for applications without their own secret. Memberships created before application secrets existed
	/// have no value for the switch, which is treated as allowed.
	/// </summary>
	public bool Verify(Application application, Membership membership, string secret)
	{
		if (!string.IsNullOrEmpty(application.SecretHash) || membership.AllowMembershipSecretForApplications == false)
		{
			return false;
		}
		
		// Constant-time comparison, so that the secret can not be guessed from response times
		if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(membership.SecretKey), Encoding.UTF8.GetBytes(secret)))
		{
			return false;
		}
		
		this.LogLegacyUsage(application);
		return true;
	}
	
	private void LogLegacyUsage(Application application)
	{
		var cacheKey = $"{CACHE_KEY}.{application.Id}";
		if (this._memoryCache.TryGetValue(cacheKey, out _))
		{
			return;
		}
		
		this._memoryCache.Set(cacheKey, true, WarningInterval);
		this._logger.LogWarning(
			"Application {ApplicationId} ({ApplicationSlug}) in membership {MembershipId} authenticated with the membership secret key; generate its own secret and migrate it",
			application.Id,
			application.Slug,
			application.MembershipId);
	}
	
	#endregion
}
