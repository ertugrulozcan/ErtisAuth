using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Infrastructure.Services
{
	public class ActiveTokenService : MembershipBoundedService<ActiveToken, ActiveTokenDto>, IActiveTokenService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="repository"></param>
		public ActiveTokenService(IMembershipService membershipService, IActiveTokensRepository repository) : base(membershipService, repository)
		{
			
		}

		#endregion
	}
}