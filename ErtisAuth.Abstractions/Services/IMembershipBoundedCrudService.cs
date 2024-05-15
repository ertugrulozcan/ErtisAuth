using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Abstractions.Services
{
	public interface IMembershipBoundedCrudService<T> : IMembershipBoundedService<T>, IDeletableMembershipBoundedService where T : IHasMembership
	{
		T Create(Utilizer utilizer, string membershipId, T model);
		
		ValueTask<T> CreateAsync(Utilizer utilizer, string membershipId, T model, CancellationToken cancellationToken = default);

		T Update(Utilizer utilizer, string membershipId, T model);
		
		ValueTask<T> UpdateAsync(Utilizer utilizer, string membershipId, T model, CancellationToken cancellationToken = default);
	}
}