using System.Collections.Generic;
using System.Threading.Tasks;
using ErtisAuth.Core.Models;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
    public interface IMembershipUsageService
    {
        IEnumerable<MembershipBoundedResource> GetMembershipBoundedResources(string membershipId, int limit = 10);
        
        Task<IEnumerable<MembershipBoundedResource>> GetMembershipBoundedResourcesAsync(string membershipId, int limit = 10);
    }
}