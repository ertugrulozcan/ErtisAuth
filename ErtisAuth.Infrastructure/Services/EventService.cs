using System;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Events;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Services
{
	public class EventService : MembershipBoundedService<ErtisAuthEventBase, EventDto>, IEventService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="repository"></param>
		public EventService(IMembershipService membershipService, IEventRepository repository) : base(membershipService, repository)
		{
			
		}

		#endregion

		#region Events

		public event EventHandler<ErtisAuthEvent> EventFired;
		
		public event EventHandler<ErtisAuthCustomEvent> CustomEventFired;

		#endregion
		
		#region Fire Methods

		public async Task<ErtisAuthEvent> FireEventAsync(object sender, ErtisAuthEvent ertisAuthEvent)
		{
			try
			{
				var insertedEvent = await this.SaveEventAsync(ertisAuthEvent);
				if (insertedEvent != null)
				{
					ertisAuthEvent.Id = insertedEvent.Id;
					this.EventFired?.Invoke(sender, ertisAuthEvent);

					return ertisAuthEvent;
				}
				else
				{
					Console.WriteLine("EventService.FireEventAsync error: Event could not inserted!");
					return null;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("EventService.FireEventAsync exception:");
				Console.WriteLine(ex);
				return null;
			}
		}

		public async Task<ErtisAuthCustomEvent> FireEventAsync(object sender, ErtisAuthCustomEvent ertisAuthCustomEvent)
		{
			try
			{
				var insertedEvent = await this.SaveEventAsync(ertisAuthCustomEvent);
				if (insertedEvent != null)
				{
					ertisAuthCustomEvent.Id = insertedEvent.Id;
					this.CustomEventFired?.Invoke(sender, ertisAuthCustomEvent);

					return ertisAuthCustomEvent;
				}
				else
				{
					Console.WriteLine("EventService.FireEventAsync error: Custom event could not inserted!");
					return null;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("EventService.FireEventAsync exception:");
				Console.WriteLine(ex);
				return null;
			}
		}

		private async Task<EventDto> SaveEventAsync(ErtisAuthEventBase ertisAuthEvent)
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

			string eventType;
			if (ertisAuthEvent.IsCustomEvent)
			{
				var ertisAuthCustomEvent = ertisAuthEvent as ErtisAuthCustomEvent;
				eventType = ertisAuthCustomEvent?.EventType;
			}
			else
			{
				var ertisAuthNativeEvent = ertisAuthEvent as ErtisAuthEvent;
				eventType = ertisAuthNativeEvent?.EventType.ToString();
			}

			if (string.IsNullOrEmpty(eventType))
			{
				eventType = "Unknown";
			}
			
			return await this.repository.InsertAsync(new EventDto
			{
				EventType = eventType,
				UtilizerId = ertisAuthEvent.UtilizerId,
				MembershipId = ertisAuthEvent.MembershipId,
				Document = documentBson,
				Prior = priorBson,
				EventTime = ertisAuthEvent.EventTime
			});
		}

		#endregion
		
		#region Get Methods

		public override ErtisAuthEventBase Get(string membershipId, string id)
		{
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var dto = this.repository.FindOne(x => x.Id == id && x.MembershipId == membershipId);
			return DtoToModel(dto);
		}

		public override async Task<ErtisAuthEventBase> GetAsync(string membershipId, string id)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var dto = await this.repository.FindOneAsync(x => x.Id == id && x.MembershipId == membershipId);
			return DtoToModel(dto);
		}
		
		public override IPaginationCollection<ErtisAuthEventBase> Get(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var paginatedDtoCollection = this.repository.Find(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
			if (paginatedDtoCollection?.Items != null)
			{
				return new PaginationCollection<ErtisAuthEventBase>
				{
					Items = paginatedDtoCollection.Items.Select(DtoToModel),
					Count = paginatedDtoCollection.Count
				};
			}
			else
			{
				return new PaginationCollection<ErtisAuthEventBase>();
			}
		}
		
		public override async Task<IPaginationCollection<ErtisAuthEventBase>> GetAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var paginatedDtoCollection = await this.repository.FindAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
			if (paginatedDtoCollection?.Items != null)
			{
				return new PaginationCollection<ErtisAuthEventBase>
				{
					Items = paginatedDtoCollection.Items.Select(DtoToModel),
					Count = paginatedDtoCollection.Count
				};
			}
			else
			{
				return new PaginationCollection<ErtisAuthEventBase>();
			}
		}

		#endregion

		#region Mapping Methods

		private static ErtisAuthEventBase DtoToModel(EventDto dto)
		{
			if (dto.IsCustomEvent)
			{
				return DtoToCustomModel(dto);
			}
			else
			{
				return DtoToNativeModel(dto);
			}
		}
		
		private static ErtisAuthEvent DtoToNativeModel(EventDto dto)
		{
			return new ErtisAuthEvent
			{
				Id = dto.Id,
				MembershipId = dto.MembershipId,
				UtilizerId = dto.UtilizerId,
				EventType = Enum.Parse<ErtisAuthEventType>(dto.EventType),
				Document = dto.Document,
				Prior = dto.Prior,
				EventTime = dto.EventTime
			};
		}
		
		private static ErtisAuthCustomEvent DtoToCustomModel(EventDto dto)
		{
			return new ErtisAuthCustomEvent
			{
				Id = dto.Id,
				MembershipId = dto.MembershipId,
				UtilizerId = dto.UtilizerId,
				EventType = dto.EventType,
				Document = dto.Document,
				Prior = dto.Prior,
				EventTime = dto.EventTime
			};			
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
			
			var result = this.repository.Query(x => x.Id == id && x.MembershipId == membershipId, 0, 1);
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
			
			var result = await this.repository.QueryAsync(x => x.Id == id && x.MembershipId == membershipId, 0, 1);
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
			
			return this.repository.Query(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
		}

		public async Task<IPaginationCollection<dynamic>> GetDynamicAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			return await this.repository.QueryAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
		}

		#endregion
	}
}