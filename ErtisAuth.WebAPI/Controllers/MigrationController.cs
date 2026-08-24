using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.WebAPI.Models.Request.Migration;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Route("api/v{v:apiVersion}/migrate")]
public class MigrationController : ControllerBase
{
	#region Services
	
	private readonly IMigrationService migrationService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="migrationService"></param>
	public MigrationController(IMigrationService migrationService)
	{
		this.migrationService = migrationService;
	}
	
	#endregion
	
	#region Methods
	
	[HttpPost]
	public async Task<IActionResult> Migrate([FromBody] MigrationModel model)
	{
		if (model.Membership == null)
		{
			throw ErtisAuthException.ValidationError(new[] { "membership is required" });
		}
		
		if (model.User == null)
		{
			throw ErtisAuthException.ValidationError(new[] { "user is required" });
		}
		
		var connectionString = this.Request.Headers["ConnectionString"].ToString();
		if (string.IsNullOrEmpty(connectionString))
		{
			throw ErtisAuthException.ValidationError(new[] { "ConnectionString must be post in header" });
		}
		
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
		
		this.Request.HttpContext.Items.Add("SysUtilizer", "migration");
		
		var migrationResult = await this.migrationService.MigrateAsync(connectionString, membership, user, application);
		
		return this.Ok(migrationResult);
	}
	
	#endregion
}