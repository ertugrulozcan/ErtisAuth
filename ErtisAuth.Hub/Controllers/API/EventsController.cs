using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Models;
using ErtisAuth.Hub.Models.DataTables;
using Ertis.MongoDB.Queries;

namespace ErtisAuth.Hub.Controllers.API
{
    [Authorized]
	[Route("api/events")]
	public class EventsController : ControllerBase
	{
		#region Constants

		private readonly string[] Columns = 
		{
			"_id",
			"utilizer_type",
			"utilizer_id",
			"name",
			"event_type",
			"event_time"
		};  

		#endregion
		
		#region Services
		
		private readonly IEventService eventService;
		private readonly IUserService userService;
		private readonly IApplicationService applicationService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="eventService"></param>
		/// <param name="userService"></param>
		/// <param name="applicationService"></param>
		public EventsController(IEventService eventService, IUserService userService, IApplicationService applicationService)
		{
			this.eventService = eventService;
			this.userService = userService;
			this.applicationService = applicationService;
		}

		#endregion
		
		#region Get DataTable

		[HttpGet]
		public async Task<ActionResult<DataTableResponseModel>> GetEventsForDataTableAsync()
		{
			try
			{
				this.ExtractPaginationParameters(this.Columns, out var skip, out var limit, out var itemCountPerPage, out var orderBy, out var sortDirection, out var searchKeyword);

				var token = this.GetBearerToken();
				var query = this.GetFilterQuery();
				
				IResponseResult<IPaginationCollection<ErtisAuthEventLog>> getEventsResponse;
				if (query == null)
				{
					getEventsResponse = await this.eventService.GetAsync(token, skip, limit, true, orderBy, sortDirection, searchKeyword);
				}
				else
				{
					getEventsResponse = await this.eventService.QueryAsync(token, query.ToString(), skip, limit, true, orderBy, sortDirection);	
				}

				if (getEventsResponse.IsSuccess)
				{
					var list = Array.Empty<object[]>();
					if (getEventsResponse.Data.Items != null)
					{
						var events = getEventsResponse.Data.Items.ToArray();
						list = await this.GetDataTablePropertiesAsync(events, token);
					}
					
					var table = new DataTableResponseModel
					{
						Data = list,
						TotalCount = getEventsResponse.Data.Count,
						FilteredCount = getEventsResponse.Data.Count,
						ItemCountPerPage = itemCountPerPage ?? Pagination.ITEM_COUNT_PER_PAGE
					};
					
					return this.Ok(table);
				}
				else
				{
					return this.Ok(new DataTableResponseModel
					{
						ErrorMessage = getEventsResponse.Message
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return this.StatusCode(500, ex.Message);
			}
		}

		private async Task<object[][]> GetDataTablePropertiesAsync(IReadOnlyList<ErtisAuthEventLog> events, TokenBase token)
		{
			var tasks = events.Select(x => this.GetUtilizerInfoAsync(x.UtilizerId, token)).ToArray();
			await Task.WhenAll(tasks);

			var table = new List<object[]>();
			for (var i = 0; i < tasks.Length; i++)
			{
				var utilizerInfo = await tasks[i];
				var ertisAuthEvent = events[i];
				var columns = new object[]
				{
					ertisAuthEvent.Id,
					utilizerInfo.UtilizerType,
					utilizerInfo.Id,
					utilizerInfo.Name,
					ertisAuthEvent.EventType,
					ertisAuthEvent.EventTime.ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("en-US"))
				};
				
				table.Add(columns);
			}

			return table.ToArray();
		}

		private async Task<UtilizerInfo> GetUtilizerInfoAsync(string utilizerId, TokenBase token)
		{
			var getUserResponse = await this.userService.GetAsync(utilizerId, token);
			if (getUserResponse.IsSuccess)
			{
				return new UtilizerInfo
				{
					Id = getUserResponse.Data.Id,
					UtilizerType = "User",
					Name = $"{getUserResponse.Data.FirstName} {getUserResponse.Data.LastName}",
					Email = getUserResponse.Data.EmailAddress,
					Username = getUserResponse.Data.Username,
					Role = getUserResponse.Data.Role
				};
			}
			
			var getApplicationResponse = await this.applicationService.GetAsync(utilizerId, token);
			if (getApplicationResponse.IsSuccess)
			{
				return new UtilizerInfo
				{
					Id = getApplicationResponse.Data.Id,
					UtilizerType = "Application",
					Name = getApplicationResponse.Data.Name,
					Role = getApplicationResponse.Data.Role
				};
			}

			return new UtilizerInfo
			{
				Id = utilizerId
			};
		}

		private IQuery GetFilterQuery()
		{
			string utilizerIdFilter = null;
			if (this.Request.Query.ContainsKey("utilizer_id"))
			{
				var utilizerIdFilterParam = this.Request.Query["utilizer_id"].ToString();
				if (!string.IsNullOrEmpty(utilizerIdFilterParam))
				{
					utilizerIdFilter = utilizerIdFilterParam;
				}
			}
			
			string eventTypeFilter = null;
			if (this.Request.Query.ContainsKey("event_type"))
			{
				var eventFilterParam = this.Request.Query["event_type"].ToString();
				if (!string.IsNullOrEmpty(eventFilterParam) && eventFilterParam != "*")
				{
					eventTypeFilter = eventFilterParam;
				}
			}

			DateTime? startTime = null;
			if (this.Request.Query.ContainsKey("start_date"))
			{
				if (long.TryParse(this.Request.Query["start_date"].ToString(), out var filterStartDateTimeStamp) && filterStartDateTimeStamp > 0)
				{
					var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(filterStartDateTimeStamp).ToLocalTime();
					startTime = dateTimeOffset.DateTime;
				}
			}
				
			DateTime? endTime = null;
			if (this.Request.Query.ContainsKey("end_date"))
			{
				if (long.TryParse(this.Request.Query["end_date"].ToString(), out var filterEndDateTimeStamp) && filterEndDateTimeStamp > 0)
				{
					var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(filterEndDateTimeStamp).ToLocalTime();
					endTime = dateTimeOffset.DateTime;
				}
			}

			var queries = new List<IQuery>();
			if (utilizerIdFilter != null)
			{
				queries.Add(QueryBuilder.Equals("utilizer_id", utilizerIdFilter));
			}
			
			if (eventTypeFilter != null)
			{
				queries.Add(QueryBuilder.Equals("event_type", eventTypeFilter));
			}

			if (startTime != null && endTime != null)
			{
				IQuery[] eventTimeQueries =
				{
					QueryBuilder.GreaterThanOrEqual(startTime.Value),
					QueryBuilder.LessThanOrEqual(endTime.Value)
				};
            
				queries.Add(QueryBuilder.Where("event_time", eventTimeQueries));
			}
			else if (startTime != null)
			{
				queries.Add(QueryBuilder.GreaterThanOrEqual("event_time", startTime.Value));
			}
			else if (endTime != null)
			{
				queries.Add(QueryBuilder.LessThanOrEqual("event_time", endTime.Value));
			}
			
			return queries.Any() ? QueryBuilder.Where(queries) : null;
		}

		#endregion
	}
}