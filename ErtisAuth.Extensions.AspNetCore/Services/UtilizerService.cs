using System.Security.Claims;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.Authorization.Extensions;

namespace ErtisAuth.Extensions.AspNetCore.Services;

public interface IUtilizerService
{
	Task<Utilizer> GetUtilizerAsync(ClaimsPrincipal claimUser, CancellationToken cancellationToken = default);
}

public class UtilizerService : IUtilizerService
{
	#region Services
	
	private readonly IUserService _userService;
	private readonly IApplicationService _applicationService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="userService"></param>
	/// <param name="applicationService"></param>
	public UtilizerService(IUserService userService, IApplicationService applicationService)
	{
		this._userService = userService;
		this._applicationService = applicationService;
	}
	
	#endregion
	
	#region Methods
	
	public async Task<Utilizer> GetUtilizerAsync(ClaimsPrincipal claimUser, CancellationToken cancellationToken = default)
	{
		var utilizerIdentity = claimUser.Identities.FirstOrDefault(x => x.NameClaimType == ClaimExtensions.UtilizerClaimName);
		if (utilizerIdentity != null)
		{
			var claimUtilizer = utilizerIdentity.ConvertToUtilizer();
			
			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch (claimUtilizer.Type)
			{
				case Utilizer.UtilizerType.User:
				{
					var user = await this._userService.GetUserAsync(claimUtilizer.MembershipId, claimUtilizer.Id, cancellationToken: cancellationToken);
					if (user == null)
					{
						throw ErtisAuthException.AccessDenied("Utilizer user not found");
					}
					
					Utilizer utilizer = user;
					utilizer.Token = claimUtilizer.Token;
					utilizer.TokenType = claimUtilizer.TokenType;
					
					var scopes = claimUtilizer.Scopes?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
					utilizer.Scopes = scopes is { Length: > 0 } ? scopes : null;
					
					return utilizer;
				}
				case Utilizer.UtilizerType.Application:
				{
					var application = await this._applicationService.GetAsync(claimUtilizer.MembershipId, claimUtilizer.Id, cancellationToken: cancellationToken);
					if (application == null)
					{
						throw ErtisAuthException.AccessDenied("Utilizer application not found");
					}
					
					return application;
				}
			}
			
			return claimUtilizer;
		}
		
		throw ErtisAuthException.AccessDenied("Unknown utilizer");
	}
	
	#endregion
}