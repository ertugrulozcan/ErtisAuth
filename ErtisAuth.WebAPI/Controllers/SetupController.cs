using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.WebAPI.Models.Setup;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

/// <summary>
/// The one-time setup of a fresh installation (first membership, administrator user, optional application).
/// Authorized by the X-Setup-Token header matching a token the operator inserted into the "setup" collection;
/// closed once the installation is set up.
/// </summary>
[ApiController]
[Route("setup")]
public class SetupController : ControllerBase
{
	#region Services
	
	private readonly ISetupService _setupService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="setupService"></param>
	public SetupController(ISetupService setupService)
	{
		this._setupService = setupService;
	}
	
	#endregion
	
	#region Methods
	
	[HttpPost]
	public async Task<IActionResult> Setup([FromBody] SetupModel model, CancellationToken cancellationToken = default)
	{
		if (model.Membership == null)
		{
			throw ErtisAuthException.ValidationError(new[] { "membership is required" });
		}
		
		if (model.User == null)
		{
			throw ErtisAuthException.ValidationError(new[] { "user is required" });
		}
		
		var setupToken = this.Request.Headers["X-Setup-Token"].ToString();
		
		var membership = new Membership
		{
			Name = model.Membership.Name,
			Slug = model.Membership.Slug,
			ExpiresIn = model.Membership.ExpiresIn,
			RefreshTokenExpiresIn = model.Membership.RefreshTokenExpiresIn,
			HashAlgorithm = model.Membership.HashAlgorithm,
			DefaultEncoding = model.Membership.DefaultEncoding,
			SecretKey = model.Membership.SecretKey
		};
		
		var user = new UserWithPassword
		{
			Username = model.User.Username,
			EmailAddress = model.User.EmailAddress,
			FirstName = model.User.FirstName,
			LastName = model.User.LastName,
			Password = model.User.Password,
			Role = model.User.Role,
			UserType = model.User.UserType,
			Forbidden = model.User.Forbidden,
			Permissions = model.User.Permissions,
			MembershipId = string.Empty
		};
		
		Application? application = null;
		if (model.Application != null)
		{
			application = new Application
			{
				Name = model.Application.Name ?? string.Empty,
				Slug = model.Application.Slug ?? string.Empty,
				Role = model.Application.Role ?? string.Empty,
				MembershipId = string.Empty
			};
		}
		
		var result = await this._setupService.SetupAsync(setupToken, membership, user, application, cancellationToken: cancellationToken);
		return this.Ok(result);
	}
	
	#endregion
}