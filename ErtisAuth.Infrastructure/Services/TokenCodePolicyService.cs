using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services;

public class TokenCodePolicyService : MembershipBoundedCrudService<TokenCodePolicy, TokenCodePolicyDto>, ITokenCodePolicyService
{
	#region Services

	private readonly IEventService _eventService;

	#endregion
	
    #region Constructors

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="membershipService"></param>
    /// <param name="eventService"></param>
    /// <param name="repository"></param>
    public TokenCodePolicyService(
	    IMembershipService membershipService,
	    IEventService eventService,
	    ICodePolicyRepository repository) : base(membershipService, repository)
    {
	    this._eventService = eventService;
	    
	    this.OnCreated += this.OnCreatedEventHandler;
	    this.OnUpdated += this.OnUpdatedEventHandler;
	    this.OnDeleted += this.OnDeletedEventHandler;
    }

    #endregion
    
    #region Read Methods
    
    public TokenCodePolicy GetBySlug(string slug, string membershipId)
    {
	    var dto = this.repository.FindOne(x => x.Slug == slug && x.MembershipId == membershipId);
	    return dto == null ? null : Mapper.Current.Map<TokenCodePolicyDto, TokenCodePolicy>(dto);
    }
		
    public async ValueTask<TokenCodePolicy> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default)
    {
	    var dto = await this.repository.FindOneAsync(x => x.Slug == slug && x.MembershipId == membershipId, cancellationToken: cancellationToken);
	    return dto == null ? null : Mapper.Current.Map<TokenCodePolicyDto, TokenCodePolicy>(dto);
    }
    
    #endregion
    
    #region Event Handlers

    private void OnCreatedEventHandler(object sender, CreateResourceEventArgs<TokenCodePolicy> eventArgs)
    {
	    this._eventService.FireEventAsync(this, new ErtisAuthEvent
	    {
		    EventType = ErtisAuthEventType.TokenCodePolicyCreated,
		    UtilizerId = eventArgs.Utilizer.Id,
		    Document = eventArgs.Resource,
		    MembershipId = eventArgs.MembershipId
	    });
    }
		
    private void OnUpdatedEventHandler(object sender, UpdateResourceEventArgs<TokenCodePolicy> eventArgs)
    {
	    this._eventService.FireEventAsync(this, new ErtisAuthEvent
	    {
		    EventType = ErtisAuthEventType.TokenCodePolicyUpdated,
		    UtilizerId = eventArgs.Utilizer.Id,
		    Document = eventArgs.Updated,
		    Prior = eventArgs.Prior,
		    MembershipId = eventArgs.MembershipId
	    });
    }
		
    private void OnDeletedEventHandler(object sender, DeleteResourceEventArgs<TokenCodePolicy> eventArgs)
    {
	    this._eventService.FireEventAsync(this, new ErtisAuthEvent
	    {
		    EventType = ErtisAuthEventType.TokenCodePolicyDeleted,
		    UtilizerId = eventArgs.Utilizer.Id,
		    Document = eventArgs.Resource,
		    MembershipId = eventArgs.MembershipId
	    });
    }

    #endregion
    
    #region Methods
	
	protected override bool ValidateModel(TokenCodePolicy model, out IEnumerable<string> errors)
	{
		var errorList = new List<string>();
		if (string.IsNullOrEmpty(model.Name))
		{
			errorList.Add("name is a required field");
		}
		
		if (string.IsNullOrEmpty(model.MembershipId))
		{
			errorList.Add("membership_id is a required field");
		}
		
		if (model.Length <= 0)
		{
			errorList.Add("Length must be greater than zero");
		}
		
		if (model.ExpiresIn <= 0)
		{
			errorList.Add("Expires in must be greater than zero");
		}
		
		errors = errorList;
		return !errors.Any();
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

	protected override bool IsAlreadyExist(TokenCodePolicy model, string membershipId, TokenCodePolicy exclude = default) =>
		this.IsAlreadyExistAsync(model, membershipId, exclude).ConfigureAwait(false).GetAwaiter().GetResult();

	protected override async Task<bool> IsAlreadyExistAsync(TokenCodePolicy model, string membershipId, TokenCodePolicy exclude = default)
	{
		if (exclude == null)
		{
			return await this.GetBySlugAsync(model.Slug, membershipId) != null;	
		}
		else
		{
			var current = await this.GetBySlugAsync(model.Slug, membershipId);
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
		return ErtisAuthException.TokenCodePolicyWithSameNameAlreadyExists(model.Name);
	}
	
	protected override ErtisAuthException GetNotFoundError(string slug)
	{
		return ErtisAuthException.TokenCodePolicyNotFound(slug);
	}

	#endregion
}