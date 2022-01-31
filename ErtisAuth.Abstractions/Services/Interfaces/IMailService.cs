using System.Threading.Tasks;
using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Abstractions.Services.Interfaces
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