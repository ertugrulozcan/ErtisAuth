using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Extensions.Mailing.Services.Interfaces;

public interface IMailService
{
	MailProviderType GetProviderType();
}