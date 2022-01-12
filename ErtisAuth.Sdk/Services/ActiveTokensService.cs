using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
    public class ActiveTokensService : ReadonlyMembershipBoundedService<ActiveToken>, IActiveTokensService
    {
        #region Properties

        protected override string Slug => "active-tokens";	

        #endregion
        
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ertisAuthOptions"></param>
        /// <param name="restHandler"></param>
        public ActiveTokensService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
        {
			
        }

        #endregion
    }
}