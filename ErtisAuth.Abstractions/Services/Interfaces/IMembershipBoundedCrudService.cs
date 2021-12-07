using System.Threading.Tasks;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IMembershipBoundedCrudService<T> : IMembershipBoundedService<T> where T : IHasMembership
	{
		T Create(Utilizer utilizer, string membershipId, T model);
		
		ValueTask<T> CreateAsync(Utilizer utilizer, string membershipId, T model);

		T Update(Utilizer utilizer, string membershipId, T model);
		
		ValueTask<T> UpdateAsync(Utilizer utilizer, string membershipId, T model);

		bool Delete(Utilizer utilizer, string membershipId, string id);

		ValueTask<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id);
	}
}