using System;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Events;
using ErtisAuth.Infrastructure.Exceptions;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Services
{
	public class EventService : MembershipBoundedService<ErtisAuthEvent, EventDto>, IEventService
	{
		#region Services

		private readonly IMembershipService membershipService;
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
			this.membershipService = membershipService;
			this.eventRepository = repository;
		}

		#endregion

		#region Events

		public event EventHandler<ErtisAuthEvent> EventFired;

		#endregion
		
		#region Methods

		public async Task FireEventAsync(object sender, ErtisAuthEvent ertisAuthEvent)
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
					UtilizerId = ertisAuthEvent.UtilizerId,
					MembershipId = ertisAuthEvent.MembershipId,
					Document = documentBson,
					Prior = priorBson,
					EventTime = ertisAuthEvent.EventTime
				});

				this.EventFired?.Invoke(sender, ertisAuthEvent);
			}
			catch (Exception ex)
			{
				Console.WriteLine("EventService.FireEventAsync exception:");
				Console.WriteLine(ex);
			}
		}

		#endregion

		#region Dynamics

		public dynamic GetDynamic(string membershipId, string id)
		{
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var result = this.eventRepository.Query(x => x.Id == id && x.MembershipId == membershipId, 0, 1);
			if (result?.Items != null && result.Items.Any())
			{
				return result.Items.FirstOrDefault();
			}

			return null;
		}

		public async Task<dynamic> GetDynamicAsync(string membershipId, string id)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var result = await this.eventRepository.QueryAsync(x => x.Id == id && x.MembershipId == membershipId, 0, 1);
			if (result?.Items != null && result.Items.Any())
			{
				return result.Items.FirstOrDefault();
			}

			return null;
		}

		public IPaginationCollection<dynamic> GetDynamic(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			return this.eventRepository.Query(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
		}

		public async Task<IPaginationCollection<dynamic>> GetDynamicAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			return await this.eventRepository.QueryAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
		}

		#endregion
	}
}