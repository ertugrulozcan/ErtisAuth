using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.UserTypes;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request.UserTypes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/user-types")]
	public class UserTypesController : QueryControllerBase
	{
		#region Services

		private readonly IUserTypeService userTypeService;
		private readonly IMembershipService membershipService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="userTypeService"></param>
		/// <param name="membershipService"></param>
		public UserTypesController(IUserTypeService userTypeService, IMembershipService membershipService)
		{
			this.userTypeService = userTypeService;
			this.membershipService = membershipService;
		}

		#endregion
		
		#region Create Methods
		
		[HttpPost]
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateUserTypeFormModel model)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				return this.MembershipNotFound(membershipId);
			}
			
			var userTypeModel = new UserType
			{
				Name = model.Name,
				Description = model.Description,
				Schema = model.Schema,
				MembershipId = membershipId
			};
			
			var userType = await this.userTypeService.CreateAsync(membershipId, userTypeModel);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{userType.Id}", userType);
		}
		
		#endregion
		
		#region Read Methods
		
		[HttpGet("{id}")]
		public async Task<ActionResult<UserType>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var user = await this.userTypeService.GetAsync(membershipId, id);
			if (user != null)
			{
				return this.Ok(user);
			}
			else
			{
				return this.UserNotFound(id);
			}
		}
		
		[HttpGet]
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var users = await this.userTypeService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(users);
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.userTypeService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateUserTypeFormModel model)
		{
			var userTypeModel = new UserType
			{
				Id = id,
				Name = model.Name,
				Description = model.Description,
				Schema = model.Schema,
				MembershipId = membershipId
			};
			
			var userType = await this.userTypeService.UpdateAsync(membershipId, userTypeModel);
			return this.Ok(userType);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id)
		{
			if (await this.userTypeService.DeleteAsync(membershipId, id))
			{
				return this.NoContent();
			}
			else
			{
				return this.UserTypeNotFound(id);
			}
		}

		#endregion
	}
}