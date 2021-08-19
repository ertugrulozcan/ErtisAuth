using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Infrastructure.Services
{
	public class RevokedTokenService : MembershipBoundedService<RevokedToken, RevokedTokenDto>, IRevokedTokenService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="repository"></param>
		public RevokedTokenService(IMembershipService membershipService, IRevokedTokensRepository repository) : base(membershipService, repository)
		{
			
		}

		#endregion
	}
}