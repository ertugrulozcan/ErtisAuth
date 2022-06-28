using System.Net.Mail;
using System.Threading.Tasks;
using ErtisAuth.Extensions.Mailkit.Models;
using MailKit.Security;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace ErtisAuth.Extensions.Mailkit.Extensions
{
    public static class SmtpServerExtensions
    {
        #region Methods

        public static async Task TestConnectionAsync(this SmtpServer server)
        {
            if (string.IsNullOrEmpty(server.Host))
            {
                throw new SmtpException("Host is required");
            }
            else if (server.Port is <= 0 or >= 65536)
            {
                throw new SmtpException("Port value is invalid");
            }
            
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

                if (!string.IsNullOrEmpty(server.Username))
                {
                    await client.AuthenticateAsync(server.Username, server.Password);    
                }
                
                await client.DisconnectAsync(true);
            }
        }

        #endregion
    }
}