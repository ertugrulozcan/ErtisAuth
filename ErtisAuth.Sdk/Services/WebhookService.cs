using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class WebhookService : MembershipBoundedService<Webhook>, IWebhookService
	{
		#region Properties

		protected override string Slug => "webhooks";	

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public WebhookService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion
	}
}