using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Mailing;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ErtisAuth.Infrastructure.Services
{
    public class MailService : IMailService
    {
        #region Methods

        public async Task SendMailAsync(
            SmtpServer server, 
            string fromName, 
            string fromAddress, 
            string toName, 
            string toAddress, 
            string subject, 
            string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(new MailboxAddress(toName, toAddress));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                if (server.TlsEnabled)
                {
                    await client.ConnectAsync(server.Host, server.Port, SecureSocketOptions.StartTlsWhenAvailable);    
                }
                else
                {
                    await client.ConnectAsync(server.Host, server.Port);
                }
                
                await client.AuthenticateAsync(server.Username, server.Password);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        #endregion
    }
}