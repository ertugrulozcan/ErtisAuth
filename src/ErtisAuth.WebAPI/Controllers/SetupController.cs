using ErtisAuth.Core.Models.Setup;
using Ertis.Core.Models;
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
[Tags("Setup")]
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
	
	/// <summary>Set up the installation</summary>
	/// <remarks>Creates the first membership, the administrator user and optionally an application of a fresh installation. Authorized by the <c>X-Setup-Token</c> header matching the token the operator inserted into the <c>setup</c> collection. **Note:** works only once; the endpoint is closed after the installation is set up.</remarks>
	/// <param name="model">Membership, administrator user and optional application</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[ProducesResponseType<SetupResult>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
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
			Name = model.Membership.Name ?? string.Empty,
			Slug = model.Membership.Slug ?? string.Empty,
			ExpiresIn = model.Membership.ExpiresIn,
			RefreshTokenExpiresIn = model.Membership.RefreshTokenExpiresIn,
			HashAlgorithm = model.Membership.HashAlgorithm,
			DefaultEncoding = model.Membership.DefaultEncoding,
			SecretKey = model.Membership.SecretKey ?? string.Empty
		};
		
		var user = new UserWithPassword
		{
			Username = model.User.Username ?? string.Empty,
			EmailAddress = model.User.EmailAddress,
			FirstName = model.User.FirstName,
			LastName = model.User.LastName,
			Password = model.User.Password,
			UserType = model.User.UserType,
			Role = string.Empty,
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