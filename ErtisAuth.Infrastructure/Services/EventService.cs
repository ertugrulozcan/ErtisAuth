using System;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Events;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Services
{
	public class EventService : MembershipBoundedService<ErtisAuthEvent, EventDto>, IEventService
	{
		#region Services

		private readonly IEventRepository eventRepository;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="repository"></param>
		public EventService(IMembershipService membershipService, IEventRepository repository) : base(membershipService, repository)
		{
			this.eventRepository = repository;
		}

		#endregion
		
		#region Methods

		public async Task FireEventAsync(ErtisAuthEvent ertisAuthEvent)
		{
			try
			{
				BsonDocument documentBson = null;
				if (ertisAuthEvent.Document != null)
				{
					string documentJson = JsonConvert.SerializeObject(ertisAuthEvent.Document);
					documentBson = BsonDocument.Parse(documentJson);
				}
			
				BsonDocument priorBson = null;
				if (ertisAuthEvent.Prior != null)
				{
					string priorJson = JsonConvert.SerializeObject(ertisAuthEvent.Prior);
					priorBson = BsonDocument.Parse(priorJson);
				}
				
				ertisAuthEvent.EventTime = DateTime.Now;
			
				await this.eventRepository.InsertAsync(new EventDto
				{
					EventType = ertisAuthEvent.EventType.ToString(),
					UserId = ertisAuthEvent.UserId,
					MembershipId = ertisAuthEvent.MembershipId,
					Document = documentBson,
					Prior = priorBson,
					EventTime = ertisAuthEvent.EventTime
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine("EventService.FireEventAsync exception:");
				Console.WriteLine(ex);
			}
		}

		#endregion	
	}
}