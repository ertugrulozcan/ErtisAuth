using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Extensions.Mailkit.Providers;

namespace ErtisAuth.Extensions.Mailkit.Services.Interfaces
{
    public interface IMailService
    {
        Task SendMailAsync(
            IMailProvider mailProvider, 
            string fromName, 
            string fromAddress, 
            string toName, 
            string toAddress,
            string subject, 
            string htmlBody,
            CancellationToken cancellationToken = default);
    }
}