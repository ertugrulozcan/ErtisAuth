using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;
using MongoDB.Driver.Core.Events;

namespace ErtisAuth.Dao.Repositories
{
	public class ActiveTokensRepository : RepositoryBase<ActiveTokenDto>, IActiveTokensRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="clientSettings"></param>
		/// <param name="eventSubscriber"></param>
		public ActiveTokensRepository(IDatabaseSettings settings, IClientSettings clientSettings, IEventSubscriber eventSubscriber) : 
			base(settings, "active_tokens", clientSettings, null, eventSubscriber)
		{
			
		}

		#endregion
	}
}