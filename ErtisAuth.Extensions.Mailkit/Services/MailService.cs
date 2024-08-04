using System;
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
            string templateId,
            IDictionary<string, string> arguments,
            CancellationToken cancellationToken = default)
        {
            switch (mailProvider.DeliveryMode)
            {
                case MailDeliveryMode.Default:
                case MailDeliveryMode.Raw:
                    await mailProvider.SendMailAsync(
                        fromName,
                        fromAddress,
                        recipients,
                        subject,
                        htmlBody,
                        cancellationToken: cancellationToken);
                    break;
                case MailDeliveryMode.Template:
                    await mailProvider.SendMailWithTemplateAsync(
                        fromName,
                        fromAddress,
                        recipients,
                        subject,
                        templateId,
                        arguments, 
                        cancellationToken: cancellationToken);
                    break;
            }
        }

        #endregion
    }
}