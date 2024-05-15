using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using Ertis.MongoDB.Queries;
using Ertis.Security.Cryptography;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request.Memberships;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Authorized]
	[RbacResource("memberships")]
	[Route("api/v{v:apiVersion}/[controller]")]
	public class MembershipsController : QueryControllerBase
	{
		#region Services

		private readonly IMembershipService membershipService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		public MembershipsController(IMembershipService membershipService)
		{
			this.membershipService = membershipService;
		}

		#endregion
		
		#region Create Methods
		
		[HttpPost]
		[RbacAction(Rbac.CrudActions.Create)]
		public async Task<IActionResult> Create([FromBody] CreateMembershipFormModel model, CancellationToken cancellationToken = default)
		{
			var membership = await this.membershipService.CreateAsync(model.ToMembership(), cancellationToken: cancellationToken);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{membership.Id}", membership);
		}
		
		#endregion
		
		#region Read Methods

		[HttpGet("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> Get([FromRoute] string id)
		{
			var membership = await this.membershipService.GetAsync(id);
			if (membership != null)
			{
				return this.Ok(membership);
			}
			else
			{
				return this.MembershipNotFound(id);
			}
		}
		
		[HttpGet]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var memberships = await this.membershipService.GetAsync(skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
			return this.Ok(memberships);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		public override async Task<IActionResult> Query(CancellationToken cancellationToken = default)
		{
			return await base.Query(cancellationToken: cancellationToken);
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields, CancellationToken cancellationToken = default)
		{
			return await this.membershipService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields, cancellationToken: cancellationToken);
		}
		
		[HttpGet("search")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Search([FromRoute] string membershipId, [FromQuery] string keyword, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(keyword.Trim()))
			{
				return this.SearchKeywordRequired();
			}
			
			this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
			this.ExtractSortingParameters(out var orderBy, out var sortDirection);
			
			return this.Ok(await this.membershipService.SearchAsync(keyword, null, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken));
		}

		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateMembershipFormModel model, CancellationToken cancellationToken = default)
		{
			var user = await this.membershipService.UpdateAsync(model.ToMembership(id), cancellationToken: cancellationToken);
			return this.Ok(user);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken cancellationToken = default)
		{
			if (await this.membershipService.DeleteAsync(id, cancellationToken: cancellationToken))
			{
				return this.NoContent();
			}
			else
			{
				return this.RoleNotFound(id);
			}
		}

		#endregion

		#region Membership Setting Methods

		[HttpGet("settings")]
		[RbacAction(Rbac.CrudActions.Read)]
		public IActionResult GetAllSettings()
		{
			return this.Ok(new
			{
				encodings = System.Text.Encoding.GetEncodings().Select(x => new
				{
					displayName = x.DisplayName,
					name = x.Name.ToUpper()
				}).ToArray(),
				defaultEncoding = Core.Constants.Defaults.DEFAULT_ENCODING.HeaderName,
				hashAlgorithms = Enum.GetNames<HashAlgorithms>().Select(x => x.Replace('_', '-')).ToArray(),
				defaultHashAlgorithm = Core.Constants.Defaults.DEFAULT_HASH_ALGORITHM.ToString().Replace('_', '-'),
				dbLocales = TextSearchLanguage.All.ToArray(),
				defaultDbLocale = TextSearchLanguage.None.ISO6391Code
			});
		}
		
		[HttpGet("settings/encodings")]
		[RbacAction(Rbac.CrudActions.Read)]
		public IActionResult GetEncodingList()
		{
			var encodings = System.Text.Encoding.GetEncodings();
			return this.Ok(encodings.Select(x => new
			{
				displayName = x.DisplayName,
				name = x.Name.ToUpper()
			}).ToArray());
		}
		
		[HttpGet("settings/encodings/default")]
		[RbacAction(Rbac.CrudActions.Read)]
		public IActionResult GetDefaultEncoding()
		{
			return this.Ok(Core.Constants.Defaults.DEFAULT_ENCODING.HeaderName);
		}
		
		[HttpGet("settings/hash-algorithms")]
		[RbacAction(Rbac.CrudActions.Read)]
		public IActionResult GetHashAlgorithmList()
		{
			var hashAlgorithms = Enum.GetNames<HashAlgorithms>().Select(x => x.Replace('_', '-'));
			return this.Ok(hashAlgorithms.ToArray());
		}
		
		[HttpGet("settings/hash-algorithms/default")]
		[RbacAction(Rbac.CrudActions.Read)]
		public IActionResult GetDefaultHashAlgorithm()
		{
			return this.Ok(Core.Constants.Defaults.DEFAULT_HASH_ALGORITHM.ToString().Replace('_', '-'));
		}
		
		[HttpGet("settings/db-locales")]
		[RbacAction(Rbac.CrudActions.Read)]
		public IActionResult GetDbLocales()
		{
			var languages = TextSearchLanguage.All;
			return this.Ok(languages.ToArray());
		}
		
		[HttpGet("settings/db-locales/default")]
		[RbacAction(Rbac.CrudActions.Read)]
		public IActionResult GetDefaultDbLocale()
		{
			return this.Ok(TextSearchLanguage.None.ISO6391Code);
		}

		#endregion
	}
}