using System.Threading.Tasks;
using ErtisAuth.Core.Models;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IMembershipBoundedCrudService<T> : IMembershipBoundedService<T> where T : IHasMembership
	{
		T Create(string membershipId, T model);
		
		Task<T> CreateAsync(string membershipId, T model);

		T Update(string membershipId, T model);
		
		Task<T> UpdateAsync(string membershipId, T model);

		bool Delete(string membershipId, string id);

		Task<bool> DeleteAsync(string membershipId, string id);
	}
}