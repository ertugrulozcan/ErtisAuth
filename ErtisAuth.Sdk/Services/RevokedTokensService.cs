using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

// ReSharper disable UnusedType.Global
namespace ErtisAuth.Sdk.Services;

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