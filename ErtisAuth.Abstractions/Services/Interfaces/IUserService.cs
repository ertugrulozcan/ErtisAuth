using System;
using System.Collections.Generic;
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
        
        Task<DynamicObject> GetAsync(string membershipId, string id);

        Task<IPaginationCollection<DynamicObject>> GetAsync(string membershipId, int? skip = null, int? limit = null, bool withCount = false, string orderBy = null, SortDirection? sortDirection = null);
		
        Task<DynamicObject> CreateAsync(Utilizer utilizer, string membershipId, DynamicObject model);
		
        Task<DynamicObject> UpdateAsync(Utilizer utilizer, string membershipId, string userId, DynamicObject model);
        
        Task<IPaginationCollection<DynamicObject>> QueryAsync(
            string membershipId, 
            string query,
            int? skip = null,
            int? limit = null,
            bool? withCount = null,
            string orderBy = null,
            SortDirection? sortDirection = null,
            IDictionary<string, bool> selectFields = null);
        
        Task<IPaginationCollection<DynamicObject>> SearchAsync(
            string membershipId, 
            string keyword, 
            int? skip = null, 
            int? limit = null,
            bool? withCount = null, 
            string sortField = null, 
            SortDirection? sortDirection = null);
        
        Task<UserWithPasswordHash> GetUserWithPasswordAsync(string membershipId, string id);
        
        Task<UserWithPasswordHash> GetUserWithPasswordAsync(string membershipId, string username, string email);
        
        Task<DynamicObject> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword);
        
        Task<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string emailAddress, string server, string host);
        
        Task SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password);

        #endregion
    }
}