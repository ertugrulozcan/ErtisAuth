using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Extensions.Hosting;
using ErtisAuth.Extensions.Mailkit.Providers;

namespace ErtisAuth.Abstractions.Services;

public interface IMailServiceBackgroundWorker : IBackgroundWorker<MailServiceBackgroundWorkerArgs>
{
    
}

public class MailServiceBackgroundWorkerArgs
{
    #region Properties
    
    public MailHook Mailhook { get; init; }
    
    public IMailProvider MailProvider { get; init; }
    
    public string UserId { get; init; }
    
    public string MembershipId { get; init; }
    
    public object Payload { get; init; }

    #endregion
}