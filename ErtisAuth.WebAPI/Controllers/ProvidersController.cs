using Ertis.Core.Models;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.WebAPI.Models.Providers;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using ErtisAuth.Integrations.OAuth.Core;
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
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<Provider>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Provider>> Get([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var provider = id.IsObjectId() ? await this._providerService.GetAsync(id, membershipId, cancellationToken: cancellationToken) : await this._providerService.GetBySlugAsync(id, membershipId, cancellationToken: cancellationToken);
		if (provider != null)
		{
			return ProviderResult(provider);
		}
		else
		{
			return this.ProviderNotFound(id);
		}
	}
	
	/// <summary>List providers</summary>
	/// <remarks>Returns the external identity providers (Google, Facebook, Apple, Microsoft) of the membership.</remarks>
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
	/// <remarks>Anonymous: returns only the public settings (name, slug, type, client id, tenant id, redirect uri) of the active providers, for the login pages (login url: <c>oauth/{slug}/login</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	[HttpGet("active-providers")]
	[Unauthorized]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetActiveProviders([FromRoute] string membershipId)
	{
		var providers = await this._providerService.GetProvidersAsync(membershipId);
		var activeProviders = providers.Where(x => x.IsActive);
		
		var publicActiveProviders = activeProviders.Select(provider =>
		{
			return provider switch
			{
				BaseAppleProvider appleProvider => new
				{
					_id = appleProvider.Id,
					name = appleProvider.Name,
					slug = appleProvider.Slug,
					type = appleProvider.Type.ToString(),
					appClientId = appleProvider.AppClientId,
					redirectUri = appleProvider.RedirectUri,
					membership_id = appleProvider.MembershipId
				},
				FacebookProvider facebookProvider => new
				{
					_id = facebookProvider.Id,
					name = facebookProvider.Name,
					slug = facebookProvider.Slug,
					type = facebookProvider.Type.ToString(),
					appClientId = facebookProvider.AppClientId,
					membership_id = facebookProvider.MembershipId
				},
				GoogleProvider googleProvider => new
				{
					_id = googleProvider.Id,
					name = googleProvider.Name,
					slug = googleProvider.Slug,
					type = googleProvider.Type.ToString(),
					appClientId = googleProvider.AppClientId,
					membership_id = googleProvider.MembershipId
				},
				MicrosoftProvider microsoftProvider => new
				{
					_id = microsoftProvider.Id,
					name = microsoftProvider.Name,
					slug = microsoftProvider.Slug,
					type = microsoftProvider.Type.ToString(),
					appClientId = microsoftProvider.AppClientId,
					tenantId = microsoftProvider.TenantId,
					membership_id = microsoftProvider.MembershipId
				},
				_ => null as dynamic
			};
		}).Where(x => x != null);
		
		return this.Ok(publicActiveProviders);
	}
	
	#endregion
	
	#region Create Methods
	
	/// <summary>Create a provider</summary>
	/// <remarks>The <c>type</c> (Google, Facebook, Microsoft, Apple or AppleNative) is required and can't be changed later; <c>name</c> defaults to the type and <c>slug</c> to the name. The slug is unique in the membership, can't be changed later and is the provider's login url (<c>oauth/{slug}/login</c>); several providers of the same type can be created. An active provider needs <c>defaultRole</c>, <c>defaultUserType</c> and <c>appClientId</c> (Apple types also <c>teamId</c>, <c>privateKey</c>, <c>privateKeyId</c> and <c>redirectUri</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="model">Provider</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[RbacAction(Rbac.CrudActions.Create)]
	[ProducesResponseType<Provider>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateProviderFormModel model, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(model.Type))
		{
			return this.ProviderTypeRequired();
		}
		
		if (!Enum.TryParse<ProviderType>(model.Type, out var providerType))
		{
			return this.UnknownProvider(model.Type);
		}
		
		Provider? provider = null;
		
		// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
		switch (providerType)
		{
			case ProviderType.Facebook:
				provider = new FacebookProvider
				{
					MembershipId = membershipId,
					Name = model.Name ?? providerType.ToString(),
					Slug = model.Slug!,
					Description = model.Description,
					DefaultRole = model.DefaultRole,
					DefaultUserType = model.DefaultUserType,
					IsActive = model.IsActive,
					TrustEmail = model.TrustEmail,
					AppClientId = model.AppClientId
				};
			break;
			case ProviderType.Google:
				provider = new GoogleProvider
				{
					MembershipId = membershipId,
					Name = model.Name ?? providerType.ToString(),
					Slug = model.Slug!,
					Description = model.Description,
					DefaultRole = model.DefaultRole,
					DefaultUserType = model.DefaultUserType,
					IsActive = model.IsActive,
					TrustEmail = model.TrustEmail,
					AppClientId = model.AppClientId
				};
			break;
			case ProviderType.Microsoft:
				provider = new MicrosoftProvider
				{
					MembershipId = membershipId,
					Name = model.Name ?? providerType.ToString(),
					Slug = model.Slug!,
					Description = model.Description,
					DefaultRole = model.DefaultRole,
					DefaultUserType = model.DefaultUserType,
					IsActive = model.IsActive,
					TrustEmail = model.TrustEmail,
					AppClientId = model.AppClientId,
					TenantId = model.TenantId
				};
			break;
			case ProviderType.Apple:
				provider = new AppleProvider
				{
					MembershipId = membershipId,
					Name = model.Name ?? providerType.ToString(),
					Slug = model.Slug!,
					Description = model.Description,
					DefaultRole = model.DefaultRole,
					DefaultUserType = model.DefaultUserType,
					IsActive = model.IsActive,
					TrustEmail = model.TrustEmail,
					AppClientId = model.AppClientId,
					TeamId = model.TeamId,
					PrivateKey = model.PrivateKey,
					PrivateKeyId = model.PrivateKeyId,
					RedirectUri = model.RedirectUri
				};
			break;
			case ProviderType.AppleNative:
				provider = new AppleNativeProvider
				{
					MembershipId = membershipId,
					Name = model.Name ?? providerType.ToString(),
					Slug = model.Slug!,
					Description = model.Description,
					DefaultRole = model.DefaultRole,
					DefaultUserType = model.DefaultUserType,
					IsActive = model.IsActive,
					TrustEmail = model.TrustEmail,
					AppClientId = model.AppClientId,
					TeamId = model.TeamId,
					PrivateKey = model.PrivateKey,
					PrivateKeyId = model.PrivateKeyId,
					RedirectUri = model.RedirectUri
				};
			break;
		}
		
		if (provider == null)
		{
			return this.UnsupportedProvider();
		}
		
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var createdProvider = await this._providerService.CreateAsync(provider, membershipId, utilizer, cancellationToken: cancellationToken);
		return new CreatedResult($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{createdProvider.Id}", createdProvider)
		{
			DeclaredType = typeof(Provider)
		};
	}
	
	#endregion
	
	#region Update Methods
	
	/// <summary>Update a provider</summary>
	/// <remarks>The provider's <c>type</c> and <c>slug</c> can't be changed (a different slug answers 400 <c>ProviderSlugCannotBeChanged</c>). Omitted fields keep their current values. **Note:** an update without any change answers 409 (<c>IdenticalDocument</c>).</remarks>
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
		var current = id.IsObjectId() ? 
			await this._providerService.GetAsync(id, membershipId, cancellationToken: cancellationToken) : 
			await this._providerService.GetBySlugAsync(id, membershipId, cancellationToken: cancellationToken);
		
		if (current == null)
		{
			return this.ProviderNotFound(id);
		}
		
		Provider? provider = null;
		
		// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
		switch (current.Type)
		{
			case ProviderType.Facebook:
				var facebookProvider = current as FacebookProvider;
				provider = new FacebookProvider
				{
					Id = current.Id,
					MembershipId = membershipId,
					Name = model.Name ?? current.Name,
					Slug = model.Slug ?? current.Slug,
					Description = model.Description,
					DefaultRole = model.DefaultRole ?? current.DefaultRole,
					DefaultUserType = model.DefaultUserType ?? current.DefaultUserType,
					IsActive = model.IsActive ?? current.IsActive,
					TrustEmail = model.TrustEmail ?? current.TrustEmail,
					AppClientId = model.AppClientId ?? facebookProvider?.AppClientId
				};
			break;
			case ProviderType.Google:
				var googleProvider = current as GoogleProvider;
				provider = new GoogleProvider
				{
					Id = current.Id,
					MembershipId = membershipId,
					Name = model.Name ?? current.Name,
					Slug = model.Slug ?? current.Slug,
					Description = model.Description,
					DefaultRole = model.DefaultRole ?? current.DefaultRole,
					DefaultUserType = model.DefaultUserType ?? current.DefaultUserType,
					IsActive = model.IsActive ?? current.IsActive,
					TrustEmail = model.TrustEmail ?? current.TrustEmail,
					AppClientId = model.AppClientId ?? googleProvider?.AppClientId
				};
			break;
			case ProviderType.Microsoft:
				var microsoftProvider = current as MicrosoftProvider;
				provider = new MicrosoftProvider
				{
					Id = current.Id,
					MembershipId = membershipId,
					Name = model.Name ?? current.Name,
					Slug = model.Slug ?? current.Slug,
					Description = model.Description,
					DefaultRole = model.DefaultRole ?? current.DefaultRole,
					DefaultUserType = model.DefaultUserType ?? current.DefaultUserType,
					IsActive = model.IsActive ?? current.IsActive,
					TrustEmail = model.TrustEmail ?? current.TrustEmail,
					AppClientId = model.AppClientId ?? microsoftProvider?.AppClientId,
					TenantId = model.TenantId ?? microsoftProvider?.TenantId
				};
			break;
			case ProviderType.Apple:
				var appleProvider = current as AppleProvider;
				provider = new AppleProvider
				{
					Id = current.Id,
					MembershipId = membershipId,
					Name = model.Name ?? current.Name,
					Slug = model.Slug ?? current.Slug,
					Description = model.Description,
					DefaultRole = model.DefaultRole ?? current.DefaultRole,
					DefaultUserType = model.DefaultUserType ?? current.DefaultUserType,
					IsActive = model.IsActive ?? current.IsActive,
					TrustEmail = model.TrustEmail ?? current.TrustEmail,
					AppClientId = model.AppClientId ?? appleProvider?.AppClientId,
					TeamId = model.TeamId ?? appleProvider?.TeamId,
					PrivateKey = model.PrivateKey ?? appleProvider?.PrivateKey,
					PrivateKeyId = model.PrivateKeyId ?? appleProvider?.PrivateKeyId,
					RedirectUri = model.RedirectUri ?? appleProvider?.RedirectUri
				};
			break;
			case ProviderType.AppleNative:
				var appleNativeProvider = current as AppleNativeProvider;
				provider = new AppleNativeProvider
				{
					Id = current.Id,
					MembershipId = membershipId,
					Name = model.Name ?? current.Name,
					Slug = model.Slug ?? current.Slug,
					Description = model.Description,
					DefaultRole = model.DefaultRole ?? current.DefaultRole,
					DefaultUserType = model.DefaultUserType ?? current.DefaultUserType,
					IsActive = model.IsActive ?? current.IsActive,
					TrustEmail = model.TrustEmail ?? current.TrustEmail,
					AppClientId = model.AppClientId ?? appleNativeProvider?.AppClientId,
					TeamId = model.TeamId ?? appleNativeProvider?.TeamId,
					PrivateKey = model.PrivateKey ?? appleNativeProvider?.PrivateKey,
					PrivateKeyId = model.PrivateKeyId ?? appleNativeProvider?.PrivateKeyId,
					RedirectUri = model.RedirectUri ?? appleNativeProvider?.RedirectUri
				};
			break;
		}
		
		if (provider == null)
		{
			return this.UnsupportedProvider();
		}
		
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var updatedProvider = await this._providerService.UpdateAsync(provider, membershipId, utilizer, cancellationToken: cancellationToken);
		return ProviderResult(updatedProvider);
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
	
	#region Helpers
	
	/// <summary>
	/// Serialized as <see cref="Provider"/>, not as its concrete type: only then the "type" discriminator is written.
	/// </summary>
	[NonAction]
	private static OkObjectResult ProviderResult(Provider provider)
	{
		return new OkObjectResult(provider)
		{
			DeclaredType = typeof(Provider)
		};
	}
	
	#endregion
}