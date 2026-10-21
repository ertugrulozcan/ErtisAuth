using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

// ReSharper disable UnusedType.Global
namespace ErtisAuth.Sdk.Services;

public class MailhookService : MembershipBoundedService<Mailhook>, IMailhookService
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
    public MailhookService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
    {
		
    }
	
    #endregion
}