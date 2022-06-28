using System.Threading.Tasks;
using ErtisAuth.Extensions.Mailkit.Models;

namespace ErtisAuth.Extensions.Mailkit.Services.Interfaces
{
    public interface IMailService
    {
        Task SendMailAsync(
            SmtpServer server, 
            string fromName, 
            string fromAddress, 
            string toName, 
            string toAddress,
            string subject, 
            string htmlBody);
    }
}