using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Providers;
using MongoDB.Driver.Core.Events;

namespace ErtisAuth.Dao.Repositories
{
	public class ProviderRepository : RepositoryBase<ProviderDto>, IProviderRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="clientSettings"></param>
		/// <param name="actionBinder"></param>
		/// <param name="eventSubscriber"></param>
		public ProviderRepository(IDatabaseSettings settings, IClientSettings clientSettings, IRepositoryActionBinder actionBinder, IEventSubscriber eventSubscriber) : 
			base(settings, "providers", clientSettings, actionBinder, eventSubscriber)
		{
			
		}

		#endregion
	}
}