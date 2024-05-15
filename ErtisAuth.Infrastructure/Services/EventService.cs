using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
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

		public async ValueTask<ErtisAuthEvent> FireEventAsync(object sender, ErtisAuthEvent ertisAuthEvent, CancellationToken cancellationToken = default)
		{
			try
			{
				var insertedEvent = await this.SaveEventAsync(ertisAuthEvent, cancellationToken: cancellationToken);
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

		public async ValueTask<ErtisAuthCustomEvent> FireEventAsync(object sender, ErtisAuthCustomEvent ertisAuthCustomEvent, CancellationToken cancellationToken = default)
		{
			try
			{
				var insertedEvent = await this.SaveEventAsync(ertisAuthCustomEvent, cancellationToken: cancellationToken);
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
		
		private async Task<EventDto> SaveEventAsync(ErtisAuthEventBase ertisAuthEvent, CancellationToken cancellationToken = default)
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

			var now = DateTime.Now;
			var utc = now.ToUniversalTime();
			var local = now.ToLocalTime();
			var timeZoneDiff = local - utc;
			ertisAuthEvent.EventTime = utc.Add(timeZoneDiff);

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

			var eventDto = new EventDto
			{
				EventType = eventType,
				UtilizerId = ertisAuthEvent.UtilizerId,
				MembershipId = ertisAuthEvent.MembershipId,
				Document = documentBson,
				Prior = priorBson,
				EventTime = ertisAuthEvent.EventTime
			};
			
			return await this.repository.InsertAsync(eventDto, cancellationToken: cancellationToken);
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

		public override async ValueTask<ErtisAuthEventBase> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default)
		{
			var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var dto = await this.repository.FindOneAsync(x => x.Id == id && x.MembershipId == membershipId, cancellationToken: cancellationToken);
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
		
		public override async ValueTask<IPaginationCollection<ErtisAuthEventBase>> GetAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection, CancellationToken cancellationToken = default)
		{
			var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var paginatedDtoCollection = await this.repository.FindAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
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
			
			var result = this.repository.Query(x => x.Id == id && x.MembershipId == membershipId, 0, 1, sorting: null);
			if (result?.Items != null && result.Items.Any())
			{
				return result.Items.FirstOrDefault();
			}

			return null;
		}

		public async ValueTask<dynamic> GetDynamicAsync(string membershipId, string id, CancellationToken cancellationToken = default)
		{
			var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var result = await this.repository.QueryAsync(x => x.Id == id && x.MembershipId == membershipId, 0, 1, sorting: null, cancellationToken: cancellationToken);
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

		public async ValueTask<IPaginationCollection<dynamic>> GetDynamicAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection, CancellationToken cancellationToken = default)
		{
			var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			return await this.repository.QueryAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		}

		#endregion
	}
}