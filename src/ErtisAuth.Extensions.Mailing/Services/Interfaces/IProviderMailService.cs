using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Extensions.Mailing.Services.Interfaces;

public interface IProviderMailService
{
	MailProviderType GetProviderType();
}