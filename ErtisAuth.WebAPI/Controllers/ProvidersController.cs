using Ertis.Core.Models;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Integrations.OAuth.Core;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.WebAPI.Models.Providers;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Providers")]
[Authorized]
[RbacResource("providers")]
[MembershipRoute("providers")]
public class ProvidersController : ControllerBase
{
	#region Services
	
	private readonly IProviderService _providerService;
	private readonly IUtilizerService _utilizerService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="providerService"></param>
	/// <param name="utilizerService"></param>
	public ProvidersController(IProviderService providerService, IUtilizerService utilizerService)
	{
		this._providerService = providerService;
		this._utilizerService = utilizerService;
	}
	
	#endregion
	
	#region Read Methods
	
	/// <summary>Get a provider</summary>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Provider id</param>
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<Provider>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Provider>> Get([FromRoute] string membershipId, [FromRoute] string id)
	{
		var provider = await this._providerService.GetAsync(id, membershipId);
		if (provider != null)
		{
			return this.Ok(provider);
		}
		else
		{
			return this.ProviderNotFound(id);
		}
	}
	
	/// <summary>List providers</summary>
	/// <remarks>Returns the external identity providers (Google, Facebook, Apple, Microsoft) of the membership; the missing ones are created as inactive.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<Provider[]>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		return this.Ok(await this._providerService.GetProvidersAsync(membershipId, cancellationToken: cancellationToken));
	}
	
	/// <summary>List active providers</summary>
	/// <remarks>Anonymous: returns only the public settings (name, client id, tenant id) of the active providers, for the login pages.</remarks>
	/// <param name="membershipId">Membership id</param>
	[HttpGet("active-providers")]
	[Unauthorized]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetActiveProviders([FromRoute] string membershipId)
	{
		var providers = await this._providerService.GetProvidersAsync(membershipId);
		var activeProviders = providers.Where(x => x.IsActive);
		return this.Ok(activeProviders.Select(x => new
		{
			_id = x.Id,
			name = x.Name,
			appClientId = x.AppClientId,
			tenantId = x.TenantId,
			membership_id = x.MembershipId
		}));
	}
	
	#endregion
	
	#region Update Methods
	
	/// <summary>Update a provider</summary>
	/// <remarks>The provider is identified by its <c>name</c> (Google, Facebook, Apple or Microsoft). Omitted <c>is_active</c>, <c>trust_email</c> and <c>private_key</c> keep their current values. **Note:** an update without any change answers 409 (<c>IdenticalDocument</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Provider id</param>
	/// <param name="model">Provider</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPut("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Update)]
	[ProducesResponseType<Provider>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateProviderFormModel model, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(model.Name))
		{
			return this.BadRequest(ErtisAuthException.ProviderNameRequired().Error);
		}
		
		if (Enum.TryParse<KnownProviders>(model.Name, true, out var providerType) && providerType != KnownProviders.ErtisAuth)
		{
			// Omitted flags and private key keep their current values
			var current = model.IsActive == null || model.TrustEmail == null || model.PrivateKey == null ? await this._providerService.GetAsync(id, membershipId, cancellationToken: cancellationToken) : null;
			var providerModel = new Provider(providerType)
			{
				Id = id,
				Description = model.Description,
				DefaultRole = model.DefaultRole,
				DefaultUserType = model.DefaultUserType,
				AppClientId = model.AppClientId,
				TenantId = model.TenantId,
				TeamId = model.TeamId,
				PrivateKey = model.PrivateKey ?? current?.PrivateKey,
				PrivateKeyId = model.PrivateKeyId,
				RedirectUri = model.RedirectUri,
				IsActive = model.IsActive ?? current?.IsActive ?? false,
				TrustEmail = model.TrustEmail ?? current?.TrustEmail ?? false,
				MembershipId = membershipId
			};
			
			var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
			var providerInstance = await this._providerService.UpdateAsync(providerModel, membershipId, utilizer, cancellationToken: cancellationToken);
			return this.Ok(providerInstance);
		}
		else
		{
			return this.BadRequest(ErtisAuthException.UnknownProvider(model.Name).Error);
		}
	}
	
	#endregion
	
	#region Delete Methods
	
	/// <summary>Delete a provider</summary>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Provider id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpDelete("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		if (await this._providerService.DeleteAsync(id, membershipId, utilizer, cancellationToken: cancellationToken))
		{
			return this.NoContent();
		}
		else
		{
			return this.ProviderNotFound(id);
		}
	}
	
	#endregion
}