using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Events.EventArgs;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
    public interface IUserService : IDeletableMembershipBoundedService
    {
        #region Events

        event EventHandler<CreateResourceEventArgs<DynamicObject>> OnCreated;
		
        event EventHandler<UpdateResourceEventArgs<DynamicObject>> OnUpdated;
		
        event EventHandler<DeleteResourceEventArgs<DynamicObject>> OnDeleted;

        #endregion
        
        #region Methods
        
        Task<DynamicObject> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default);

        Task<IPaginationCollection<DynamicObject>> GetAsync(string membershipId, int? skip = null, int? limit = null, bool withCount = false, string orderBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default);

        Task<User> GetByUsernameOrEmailAddressAsync(string membershipId, string usernameOrEmailAddress);
        
        Task<DynamicObject> CreateAsync(Utilizer utilizer, string membershipId, DynamicObject model, string host = null, CancellationToken cancellationToken = default);
		
        Task<string> SendActivationMailAsync(string membershipId, string userId, string host = null, CancellationToken cancellationToken = default);
        
        Task<DynamicObject> UpdateAsync(Utilizer utilizer, string membershipId, string userId, DynamicObject model, bool fireEvent = true, CancellationToken cancellationToken = default);
        
        Task<IPaginationCollection<DynamicObject>> QueryAsync(
            string membershipId, 
            string query,
            int? skip = null,
            int? limit = null,
            bool? withCount = null,
            string orderBy = null,
            SortDirection? sortDirection = null,
            IDictionary<string, bool> selectFields = null, 
            CancellationToken cancellationToken = default);
        
        Task<IPaginationCollection<DynamicObject>> SearchAsync(
            string membershipId, 
            string keyword, 
            int? skip = null, 
            int? limit = null,
            bool? withCount = null, 
            string sortField = null, 
            SortDirection? sortDirection = null, 
            CancellationToken cancellationToken = default);
        
        Task<UserWithPasswordHash> GetUserWithPasswordAsync(string membershipId, string id, CancellationToken cancellationToken = default);
        
        Task<UserWithPasswordHash> GetUserWithPasswordAsync(string membershipId, string username, string email, CancellationToken cancellationToken = default);
        
        Task<DynamicObject> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword, CancellationToken cancellationToken = default);
        
        Task<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string emailAddress, string host, CancellationToken cancellationToken = default);
        
        Task SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password, CancellationToken cancellationToken = default);

        Task<bool> CheckPasswordAsync(Utilizer utilizer, string password, CancellationToken cancellationToken = default);
        
        dynamic Aggregate(string membershipId, string aggregationStagesJson);

        Task<dynamic> AggregateAsync(string membershipId, string aggregationStagesJson, CancellationToken cancellationToken = default);
        
        Task<User> ActivateUserAsync(Utilizer utilizer, string membershipId, string activationCode, CancellationToken cancellationToken = default);
        
        #endregion
    }
}