using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class EventService : ReadonlyMembershipBoundedService<ErtisAuthEventLog>, IEventService
	{
		#region Properties

		protected override string Slug => "events";	

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public EventService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion

		#region Fire Custom Event Methods

		public IResponseResult<ErtisAuthCustomEvent> FireCustomEvent(string eventType, string utilizerId, object document, object prior, TokenBase token) =>
			this.FireCustomEventAsync(eventType, utilizerId, document, prior, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<ErtisAuthCustomEvent>> FireCustomEventAsync(string eventType, string utilizerId, object document, object prior, TokenBase token)
		{
			var ertisAuthCustomEvent = new ErtisAuthCustomEvent
			{
				EventType = eventType,
				UtilizerId = utilizerId,
				Document = document,
				Prior = prior,
				MembershipId = this.MembershipId
			};
			
			return await this.ExecuteRequestAsync<ErtisAuthCustomEvent>(
				HttpMethod.Post, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/events", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(ertisAuthCustomEvent));	
		}
		
		#endregion
	}
}