using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class TokenCodePolicyService : MembershipBoundedCrudService<TokenCodePolicy>, ITokenCodePolicyService
{
	#region Services
	
	private readonly IEventService _eventService;
	private readonly ILogger<TokenCodePolicyService> _logger;
	
	#endregion
	
    #region Constructors
	
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="membershipService"></param>
    /// <param name="eventService"></param>
    /// <param name="repository"></param>
    /// <param name="logger"></param>
    public TokenCodePolicyService(
	    IMembershipService membershipService,
	    IEventService eventService,
	    ICodePolicyRepository repository,
	    ILogger<TokenCodePolicyService> logger) : base(membershipService, repository)
    {
	    this._eventService = eventService;
	    this._logger = logger;
	    
	    this.OnCreated += this.OnCreatedEventHandler;
	    this.OnUpdated += this.OnUpdatedEventHandler;
	    this.OnDeleted += this.OnDeletedEventHandler;
    }
	
    #endregion
    
    #region Read Methods
    
    public async Task<TokenCodePolicy?> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default)
    {
	    return await this._repository.FindOneAsync(x => x.Slug == slug && x.MembershipId == membershipId, cancellationToken: cancellationToken);
    }
    
    #endregion
    
    #region Event Handlers
	
	private async void OnCreatedEventHandler(object? sender, CreateResourceEventArgs<TokenCodePolicy> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.TokenCodePolicyCreated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Resource);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "TokenCodePolicyService.OnCreatedEventHandler occured an error");
		}
	}
	
	private async void OnUpdatedEventHandler(object? sender, UpdateResourceEventArgs<TokenCodePolicy> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.TokenCodePolicyUpdated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Updated, eventArgs.Prior);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "TokenCodePolicyService.OnUpdatedEventHandler occured an error");
		}
	}
	
	private async void OnDeletedEventHandler(object? sender, DeleteResourceEventArgs<TokenCodePolicy> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.TokenCodePolicyDeleted, eventArgs.Utilizer, eventArgs.MembershipId, null, eventArgs.Resource);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "TokenCodePolicyService.OnDeletedEventHandler occured an error");
		}
	}
	
    #endregion
    
    #region Methods
	
	/// <summary>
	/// The membership refers to its token code policy by slug (Membership.CodePolicy).
	/// </summary>
	private async Task<bool> IsUsedByMembershipAsync(TokenCodePolicy policy, string membershipId, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		return membership?.CodePolicy == policy.Slug;
	}
	
	public override async Task<TokenCodePolicy> UpdateAsync(TokenCodePolicy model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		// A renamed policy gets a new slug; the policy in use keeps its slug, so the membership's reference stays valid
		var current = await this.GetAsync(model.Id, membershipId, cancellationToken: cancellationToken);
		if (current != null && await this.IsUsedByMembershipAsync(current, membershipId, cancellationToken: cancellationToken))
		{
			model.Slug = current.Slug;
		}
		
		return await base.UpdateAsync(model, membershipId, utilizer, cancellationToken: cancellationToken);
	}
	
	public override async Task<bool> DeleteAsync(string id, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		await this.EnsureNotInUseAsync([id], membershipId, cancellationToken: cancellationToken);
		return await base.DeleteAsync(id, membershipId, utilizer, cancellationToken: cancellationToken);
	}
	
	public override async Task<bool?> BulkDeleteAsync(string[] ids, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		await this.EnsureNotInUseAsync(ids, membershipId, cancellationToken: cancellationToken);
		return await base.BulkDeleteAsync(ids, membershipId, utilizer, cancellationToken: cancellationToken);
	}
	
	private async Task EnsureNotInUseAsync(IEnumerable<string> ids, string membershipId, CancellationToken cancellationToken = default)
	{
		foreach (var id in ids)
		{
			var policy = await this.GetAsync(id, membershipId, cancellationToken: cancellationToken);
			if (policy != null && await this.IsUsedByMembershipAsync(policy, membershipId, cancellationToken: cancellationToken))
			{
				throw ErtisAuthException.TokenCodePolicyInUse(policy.Slug);
			}
		}
	}
	
	protected override Task<IEnumerable<string>> ValidateModelAsync(TokenCodePolicy model, CancellationToken cancellationToken = default)
	{
		var errorList = new List<string>();
		if (string.IsNullOrEmpty(model.Name))
		{
			errorList.Add("Name is required");
		}
		
		if (!model.Slug.IsValidSlug(out var error))
		{
			errorList.Add(error!);
		}
		
		if (string.IsNullOrEmpty(model.MembershipId))
		{
			errorList.Add("Membership id is required");
		}
		
		if (model.Length <= 0)
		{
			errorList.Add("Length must be greater than zero");
		}
		
		if (model.ExpiresIn <= 0)
		{
			errorList.Add("Expires in must be greater than zero");
		}
		
		return Task.FromResult<IEnumerable<string>>(errorList);
	}
	
	protected override void Overwrite(TokenCodePolicy destination, TokenCodePolicy source)
	{
		destination.Id = source.Id;
		destination.MembershipId = source.MembershipId;
		destination.Sys = source.Sys;
		
		if (this.IsIdentical(destination, source))
		{
			throw ErtisAuthException.IdenticalDocument();
		}
		
		if (string.IsNullOrEmpty(destination.Name))
		{
			destination.Name = source.Name;
		}
		
		if (string.IsNullOrEmpty(destination.Description))
		{
			destination.Description = source.Description;
		}
	}
	
	protected override async Task<bool> IsAlreadyExistAsync(TokenCodePolicy model, string membershipId, TokenCodePolicy? exclude = null, CancellationToken cancellationToken = default)
	{
		if (exclude == null)
		{
			return await this.GetBySlugAsync(model.Slug, membershipId, cancellationToken: cancellationToken) != null;	
		}
		else
		{
			var current = await this.GetBySlugAsync(model.Slug, membershipId, cancellationToken: cancellationToken);
			if (current != null)
			{
				return current.Id != exclude.Id;	
			}
			else
			{
				return false;
			}
		}
	}
	
	protected override ErtisAuthException GetAlreadyExistError(TokenCodePolicy model)
	{
		return ErtisAuthException.TokenCodePolicyAlreadyExists(model.Slug);
	}
	
	protected override ErtisAuthException GetNotFoundError(string slug)
	{
		return ErtisAuthException.TokenCodePolicyNotFound(slug);
	}
	
	#endregion
}