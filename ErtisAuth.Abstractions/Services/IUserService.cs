using Ertis.Core.Collections;
using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable EventNeverSubscribedTo.Global
namespace ErtisAuth.Abstractions.Services;

public interface IUserService : IDeletableMembershipBoundedService
{
    #region Events
    
    event EventHandler<CreateResourceEventArgs<DynamicObject>>? OnCreated;
    event EventHandler<UpdateResourceEventArgs<DynamicObject>>? OnUpdated;
    event EventHandler<DeleteResourceEventArgs<DynamicObject>>? OnDeleted;
    
    #endregion
    
    #region Methods
    
    Task<User?> GetUserAsync(string id, string membershipId, CancellationToken cancellationToken = default);
    
    Task<DynamicObject?> GetAsync(string id, string membershipId, CancellationToken cancellationToken = default);
    
    Task<IPaginationCollection<DynamicObject>> GetAsync(string membershipId, int? skip = null, int? limit = null, bool withCount = false, string? orderBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default);
    
    Task<User?> GetByUsernameOrEmailAddressAsync(string usernameOrEmailAddress, string membershipId);
    
    Task<DynamicObject> CreateAsync(DynamicObject model, string membershipId, Utilizer utilizer, string? host = null, CancellationToken cancellationToken = default);
    
    Task<DynamicObject?> UpdateAsync(DynamicObject model, string userId, string membershipId, Utilizer utilizer, bool fireEvent = true, CancellationToken cancellationToken = default);
    
    Task<string?> SendActivationMailAsync(string userId, string membershipId, string? host = null, CancellationToken cancellationToken = default);
    
    Task<IPaginationCollection<DynamicObject>> QueryAsync(
        string query,
        string membershipId, 
        int? skip = null, 
        int? limit = null, 
        bool? withCount = null, 
        string? orderBy = null, 
        SortDirection? sortDirection = null, 
        IDictionary<string, bool>? selectFields = null, 
        string? locale = null, 
        CancellationToken cancellationToken = default);
    
    Task<IPaginationCollection<DynamicObject>> SearchAsync(
        string keyword, 
        string membershipId, 
        int? skip = null, 
        int? limit = null, 
        bool? withCount = null, 
        string? sortField = null, 
        SortDirection? sortDirection = null, 
        CancellationToken cancellationToken = default);
    
    Task<UserWithPasswordHash?> GetUserWithPasswordAsync(string id, string membershipId, CancellationToken cancellationToken = default);
    
    Task<UserWithPasswordHash?> GetUserWithPasswordAsync(string username, string email, string membershipId, CancellationToken cancellationToken = default);
    
    string CalculatePasswordHash(string password, Membership membership);

    bool VerifyPassword(string password, string? passwordHash, Membership membership);

    Task<DynamicObject> ChangePasswordAsync(string userId, string membershipId, string newPassword, Utilizer utilizer, CancellationToken cancellationToken = default);
    
    Task<bool> CheckPasswordAsync(string password, Utilizer utilizer, CancellationToken cancellationToken = default);
    
    Task<dynamic> AggregateAsync(string aggregationStagesJson, string membershipId, CancellationToken cancellationToken = default);
    
    Task<User?> ActivateUserAsync(string activationCode, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
    
    Task<User?> ActivateUserByIdAsync(string userId, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
    
    Task<User?> FreezeUserByIdAsync(string userId, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
    
    #endregion
}