using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Memberships;
using MongoDB.Driver.Core.Events;

namespace ErtisAuth.Dao.Repositories
{
	public class MembershipRepository : RepositoryBase<MembershipDto>, IMembershipRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="clientSettings"></param>
		/// <param name="actionBinder"></param>
		/// <param name="eventSubscriber"></param>
		public MembershipRepository(IDatabaseSettings settings, IClientSettings clientSettings, IRepositoryActionBinder actionBinder, IEventSubscriber eventSubscriber) : 
			base(settings, "memberships", clientSettings, actionBinder, eventSubscriber)
		{
			
		}

		#endregion
	}
}