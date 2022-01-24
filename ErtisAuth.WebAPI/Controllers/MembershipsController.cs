using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Memberships;
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
		public async Task<IActionResult> Create([FromBody] CreateMembershipFormModel model)
		{
			var membershipModel = new Membership
			{
				Name = model.Name,
				ExpiresIn = model.ExpiresIn,
				RefreshTokenExpiresIn = model.RefreshTokenExpiresIn,
				SecretKey = model.SecretKey,
				HashAlgorithm = model.HashAlgorithm,
				DefaultEncoding = model.DefaultEncoding,
				UserType = model.UserType
			};
			
			var membership = await this.membershipService.CreateAsync(membershipModel);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{membership.Id}", membership);
		}
		
		#endregion
		
		#region Read Methods

		[HttpGet("{id}")]
		[RbacObject("id")]
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
		public async Task<IActionResult> Get()
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var memberships = await this.membershipService.GetAsync(skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(memberships);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		public override async Task<IActionResult> Query()
		{
			return await base.Query();
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.membershipService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		[HttpGet("search")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Search([FromRoute] string membershipId, [FromQuery] string keyword)
		{
			if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(keyword.Trim()))
			{
				return this.SearchKeywordRequired();
			}
			
			this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
			this.ExtractSortingParameters(out var orderBy, out var sortDirection);
			
			return this.Ok(await this.membershipService.SearchAsync(keyword, null, skip, limit, withCount, orderBy, sortDirection));
		}

		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateMembershipFormModel model)
		{
			var membershipModel = new Membership
			{
				Id = id,
				ExpiresIn = model.ExpiresIn,
				RefreshTokenExpiresIn = model.RefreshTokenExpiresIn,
				UserType = model.UserType
			};
			
			var user = await this.membershipService.UpdateAsync(membershipModel);
			return this.Ok(user);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string id)
		{
			if (await this.membershipService.DeleteAsync(id))
			{
				return this.NoContent();
			}
			else
			{
				return this.RoleNotFound(id);
			}
		}

		#endregion
	}
}