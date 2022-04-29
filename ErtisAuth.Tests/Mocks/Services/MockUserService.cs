using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Schema.Dynamics;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Events.EventArgs;

namespace ErtisAuth.Tests.Mocks.Services
{
    public class MockUserService : IUserService
    {
        public event EventHandler<CreateResourceEventArgs<DynamicObject>> OnCreated;
        
        public event EventHandler<UpdateResourceEventArgs<DynamicObject>> OnUpdated;
        
        public event EventHandler<DeleteResourceEventArgs<DynamicObject>> OnDeleted;

        private void PreventNeverUsedEventsWarning()
        {
            this.OnCreated?.Invoke(this, null);
            this.OnUpdated?.Invoke(this, null);
            this.OnDeleted?.Invoke(this, null);
        }
        
        public Task<DynamicObject> GetAsync(string membershipId, string id)
        {
            throw new NotImplementedException();
        }

        public Task<IPaginationCollection<DynamicObject>> GetAsync(string membershipId, int? skip = null, int? limit = null, bool withCount = false, string orderBy = null,
            SortDirection? sortDirection = null)
        {
            throw new NotImplementedException();
        }

        public Task<DynamicObject> CreateAsync(Utilizer utilizer, string membershipId, DynamicObject model)
        {
            throw new NotImplementedException();
        }

        public Task<DynamicObject> UpdateAsync(Utilizer utilizer, string membershipId, string userId, DynamicObject model)
        {
            throw new NotImplementedException();
        }

        public bool Delete(Utilizer utilizer, string membershipId, string id)
        {
            throw new NotImplementedException();
        }

        ValueTask<bool> IDeletableMembershipBoundedService.DeleteAsync(Utilizer utilizer, string membershipId, string id)
        {
            throw new NotImplementedException();
        }

        public bool? BulkDelete(Utilizer utilizer, string membershipId, string[] ids)
        {
            throw new NotImplementedException();
        }

        ValueTask<bool?> IDeletableMembershipBoundedService.BulkDeleteAsync(Utilizer utilizer, string membershipId, string[] ids)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id)
        {
            throw new NotImplementedException();
        }

        public Task<bool?> BulkDeleteAsync(Utilizer utilizer, string membershipId, string[] ids)
        {
            throw new NotImplementedException();
        }

        public Task<IPaginationCollection<DynamicObject>> QueryAsync(string membershipId, string query, int? skip = null, int? limit = null, bool? withCount = null,
            string orderBy = null, SortDirection? sortDirection = null, IDictionary<string, bool> selectFields = null)
        {
            throw new NotImplementedException();
        }

        public Task<IPaginationCollection<DynamicObject>> SearchAsync(string membershipId, string keyword, int? skip = null, int? limit = null, bool? withCount = null,
            string sortField = null, SortDirection? sortDirection = null)
        {
            throw new NotImplementedException();
        }

        public Task<UserWithPasswordHash> GetUserWithPasswordAsync(string id, string membershipId)
        {
            throw new NotImplementedException();
        }

        public Task<UserWithPasswordHash> GetUserWithPasswordAsync(string username, string email, string membershipId)
        {
            throw new NotImplementedException();
        }

        public Task<DynamicObject> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword)
        {
            throw new NotImplementedException();
        }

        public Task<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string emailAddress, string server, string host)
        {
            throw new NotImplementedException();
        }

        public Task SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress,
            string password)
        {
            throw new NotImplementedException();
        }
    }
}