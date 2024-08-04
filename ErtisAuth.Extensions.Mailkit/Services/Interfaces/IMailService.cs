using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Extensions.Mailkit.Models;
using ErtisAuth.Extensions.Mailkit.Providers;

namespace ErtisAuth.Extensions.Mailkit.Services.Interfaces
{
    public interface IMailService
    {
        Task SendMailAsync(
            IMailProvider mailProvider, 
            string fromName, 
            string fromAddress, 
            IEnumerable<Recipient> recipients,
            string subject, 
            string htmlBody,
            string templateId,
            IDictionary<string, string> arguments,
            CancellationToken cancellationToken = default);
    }
}