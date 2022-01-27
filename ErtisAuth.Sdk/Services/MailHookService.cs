using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
    public class MailHookService : MembershipBoundedService<MailHook>, IMailHookService
    {
        #region Properties

        protected override string Slug => "mailhooks";	

        #endregion
		
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ertisAuthOptions"></param>
        /// <param name="restHandler"></param>
        public MailHookService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
        {
			
        }

        #endregion
    }
}