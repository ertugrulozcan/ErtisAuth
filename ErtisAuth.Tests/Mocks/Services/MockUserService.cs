using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Resources;
using Ertis.Schema.Dynamics.Legacy;
using Ertis.Schema.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Events.EventArgs;

namespace ErtisAuth.Tests.Mocks.Services
{
    public class MockUserService : IUserService
    {
        #region Mock Data

        private List<User> MockUsers => new()
        {
            new User
            {
                Id = "user_1",
                MembershipId = "test_membership",
                FirstName = "Rick",
                LastName = "Sanchez",
                Username = "rick.sanchez",
                EmailAddress = "rick.sanchez@adultswim.com",
                Role = "admin",
                UserType = "user",
                SourceProvider = "ErtisAuth",
                IsActive = true,
                Sys = new SysModel
                {
                    CreatedAt = new DateTime(),
                    CreatedBy = "admin"
                }
            },
            new User
            {
                Id = "user_2",
                MembershipId = "test_membership",
                FirstName = "Morty",
                LastName = "Smith",
                Username = "morty.smith",
                EmailAddress = "morty.smith@adultswim.com",
                Role = "admin",
                UserType = "user",
                SourceProvider = "ErtisAuth",
                IsActive = true,
                Sys = new SysModel
                {
                    CreatedAt = new DateTime(),
                    CreatedBy = "admin"
                }
            }
        };

        #endregion
        
        #region Events

        public event EventHandler<CreateResourceEventArgs<DynamicObject>> OnCreated;
        
        public event EventHandler<UpdateResourceEventArgs<DynamicObject>> OnUpdated;
        
        public event EventHandler<DeleteResourceEventArgs<DynamicObject>> OnDeleted;

        #endregion
        
        #region Constructors

        public MockUserService()
        {
            PreventNeverUsedEventsWarning();
        }

        #endregion
        
        #region Methods
        
        private void PreventNeverUsedEventsWarning()
        {
            this.OnCreated?.Invoke(this, null);
            this.OnUpdated?.Invoke(this, null);
            this.OnDeleted?.Invoke(this, null);
        }
        
        public async Task<DynamicObject> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            var user = this.MockUsers.FirstOrDefault(x => x.MembershipId == membershipId && x.Id == id);
            return user != null ? new DynamicObject(user) : null;
        }

        public async Task<User> GetFromCacheAsync(string membershipId, string id, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return this.MockUsers.FirstOrDefault(x => x.MembershipId == membershipId && x.Id == id);
        }

        public async Task<IPaginationCollection<DynamicObject>> GetAsync(
            string membershipId, 
            int? skip = null, 
            int? limit = null, 
            bool withCount = false, 
            string orderBy = null,
            SortDirection? sortDirection = null, 
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return new PaginationCollection<DynamicObject>
            {
                Count = this.MockUsers.Count,
                Items = this.MockUsers
                    .Where(x => x.MembershipId == membershipId)
                    .Skip(skip ?? 0)
                    .Take(limit ?? 500)
                    .Select(x => new DynamicObject(x))
            };
        }
        
        public async Task<User> GetByUsernameOrEmailAddressAsync(string membershipId, string usernameOrEmailAddress)
        {
            await Task.CompletedTask;
            return this.MockUsers.FirstOrDefault(x =>
                x.MembershipId == membershipId && 
                x.Username == usernameOrEmailAddress ||
                x.EmailAddress == usernameOrEmailAddress);
        }

        public async Task<DynamicObject> CreateAsync(
            Utilizer utilizer, 
            string membershipId, 
            DynamicObject model, 
            string host = null, 
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            var user = model.Deserialize<User>();
            user.MembershipId = membershipId;
            this.MockUsers.Add(user);
            return model;
        }

        public async Task<DynamicObject> UpdateAsync(
            Utilizer utilizer, 
            string membershipId, 
            string userId, 
            DynamicObject model, 
            bool fireEvent = true, 
            CancellationToken cancellationToken = default)
        {
            var current = await this.GetAsync(membershipId, userId, cancellationToken: cancellationToken);
            if (current != null)
            {
                current.Merge(model);
                this.MockUsers[this.MockUsers.FindIndex(x => x.Id == userId)] = current.Deserialize<User>();
                return current;
            }
            else
            {
                return null;
            }
        }

        public bool Delete(Utilizer utilizer, string membershipId, string id)
        {
            var index = this.MockUsers.FindIndex(x => x.Id == id);
            if (index >= 0)
            {
                this.MockUsers.RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        async ValueTask<bool> IDeletableMembershipBoundedService.DeleteAsync(
            Utilizer utilizer, 
            string membershipId, 
            string id, 
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return this.Delete(utilizer, membershipId, id);
        }

        public bool? BulkDelete(
            Utilizer utilizer, 
            string membershipId, 
            string[] ids)
        {
            var allDeleted = true;
            foreach (var id in ids)
            {
                allDeleted = allDeleted && this.Delete(utilizer, membershipId, id);
            }

            return allDeleted;
        }

        async ValueTask<bool?> IDeletableMembershipBoundedService.BulkDeleteAsync(
            Utilizer utilizer, 
            string membershipId, 
            string[] ids, 
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return this.BulkDelete(utilizer, membershipId, ids);
        }

        public async Task<bool> DeleteAsync(
            Utilizer utilizer, 
            string membershipId, 
            string id, 
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return this.Delete(utilizer, membershipId, id);
        }

        public async Task<bool?> BulkDeleteAsync(
            Utilizer utilizer, 
            string membershipId, 
            string[] ids, 
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return this.BulkDelete(utilizer, membershipId, ids);
        }

        public async Task<IPaginationCollection<DynamicObject>> QueryAsync(
            string membershipId, 
            string query, 
            int? skip = null, 
            int? limit = null, 
            bool? withCount = null,
            string orderBy = null, 
            SortDirection? sortDirection = null, 
            IDictionary<string, bool> selectFields = null, 
            CancellationToken cancellationToken = default)
        {
            return await this.GetAsync(
                membershipId, 
                skip, 
                limit, 
                withCount ?? false, 
                orderBy, 
                sortDirection,
                cancellationToken: cancellationToken);
        }

        public async Task<IPaginationCollection<DynamicObject>> SearchAsync(
            string membershipId, 
            string keyword, 
            int? skip = null, 
            int? limit = null, 
            bool? withCount = null,
            string orderBy = null, 
            SortDirection? sortDirection = null, 
            CancellationToken cancellationToken = default)
        {
            return await this.GetAsync(
                membershipId, 
                skip, 
                limit, 
                withCount ?? false, 
                orderBy, 
                sortDirection,
                cancellationToken: cancellationToken);
        }

        public async Task<UserWithPasswordHash> GetUserWithPasswordAsync(
            string id, 
            string membershipId, 
            CancellationToken cancellationToken = default)
        {
            return (await this.GetAsync(membershipId, id, cancellationToken: cancellationToken)).Deserialize<UserWithPasswordHash>();
        }

        public async Task<UserWithPasswordHash> GetUserWithPasswordAsync(
            string username, 
            string email, 
            string membershipId, 
            CancellationToken cancellationToken = default)
        {
            return (await this.GetByUsernameOrEmailAddressAsync(membershipId, username)) as UserWithPasswordHash;
        }
        
        public Task<User> ActivateUserAsync(
            Utilizer utilizer, 
            string membershipId, 
            string activationCode, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        public Task<User> ActivateUserByIdAsync(
            Utilizer utilizer, 
            string membershipId, 
            string userId, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        public Task<User> FreezeUserByIdAsync(
            Utilizer utilizer, 
            string membershipId, 
            string userId, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DynamicObject> ChangePasswordAsync(
            Utilizer utilizer, 
            string membershipId, 
            string userId, 
            string newPassword, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ResetPasswordToken> ResetPasswordAsync(
            Utilizer utilizer, 
            string membershipId, 
            string emailAddress, 
            string host, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<User> VerifyResetTokenAsync(
            string membershipId, 
            string resetToken, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetPasswordAsync(
            Utilizer utilizer, 
            string membershipId, 
            string resetToken, 
            string usernameOrEmailAddress,
            string password, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckPasswordAsync(
            Utilizer utilizer, 
            string password, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public dynamic Aggregate(string membershipId, string aggregationStagesJson)
        {
            throw new NotImplementedException();
        }

        public Task<dynamic> AggregateAsync(
            string membershipId, 
            string aggregationStagesJson, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        public Task<string> SendActivationMailAsync(
            string membershipId, 
            string userId, 
            string host = null, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        public ResetPasswordToken GenerateResetPasswordToken(User user, Membership membership, bool asBase64 = false)
        {
            throw new NotImplementedException();
        }
        
        #endregion
    }
}