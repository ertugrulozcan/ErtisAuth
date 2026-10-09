using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Extensions.Mailkit.Models;
using ErtisAuth.Extensions.Mailkit.Providers;
using ErtisAuth.Extensions.Mailkit.Services.Interfaces;

namespace ErtisAuth.Extensions.Mailkit.Services
{
    public class MailService : IMailService
    {
        #region Methods

        public async Task SendMailAsync(
            IMailProvider mailProvider, 
            string fromName, 
            string fromAddress, 
            IEnumerable<Recipient> recipients,
            string subject, 
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            await mailProvider.SendMailAsync(
                fromName,
                fromAddress,
                recipients,
                subject,
                htmlBody,
                cancellationToken: cancellationToken);
        }

        #endregion
    }
}