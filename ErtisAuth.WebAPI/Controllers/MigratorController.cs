using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.WebAPI.Models.Request.Migration;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}/migrate")]
	public class MigratorController : ControllerBase
	{
		#region Services

		private readonly IMigrationService migrationService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="migrationService"></param>
		public MigratorController(IMigrationService migrationService)
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

			if (!this.Request.Headers.ContainsKey("ConnectionString"))
			{
				throw ErtisAuthException.ValidationError(new[] { "ConnectionString must be post in header" });
			}

			var connectionString = this.Request.Headers["ConnectionString"];
			
			var membership = new Membership
			{
				Name = model.Membership.Name,
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
				PasswordHash = model.User.Password
			};

			var migrationResult = await this.migrationService.MigrateAsync(connectionString, membership, user);
			
			return this.Ok(migrationResult);
		}
		
		#endregion
	}
}