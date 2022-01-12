using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
    public class RevokedTokensService : ReadonlyMembershipBoundedService<RevokedToken>, IRevokedTokensService
    {
        #region Properties

        protected override string Slug => "revoked-tokens";	

        #endregion
        
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ertisAuthOptions"></param>
        /// <param name="restHandler"></param>
        public RevokedTokensService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
        {
			
        }

        #endregion
    }
}