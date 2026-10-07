using Ertis.Net.Rest;
using ErtisAuth.Extensions.Mailing.MailChimp;
using ErtisAuth.Extensions.Mailing.Services;
using ErtisAuth.Extensions.Mailing.Services.Interfaces;
using ErtisAuth.Extensions.Mailing.SmtpServer;
using NSubstitute;
using Recipient = ErtisAuth.Core.Models.Mailing.Recipient;

namespace ErtisAuth.Infrastructure.Tests.Mailing;

/// <summary>
/// MailHookService sends through the non-generic mail service interfaces; their default implementations cast the provider
/// to the provider type of the service, and a provider of another type is rejected before anything is sent.
/// </summary>
public class MailServiceProviderTypeTests
{
	#region Fields
	
	private static readonly Recipient[] Recipients = [new() { DisplayName = "John Doe", EmailAddress = "john.doe@example.com" }];
	
	#endregion
	
	#region Methods
	
	[Fact]
	public async Task RawMailService_WithAnotherProviderType_ThrowsArgumentException()
	{
		IRawMailService service = new SmtpServerService();
		
		var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SendMailAsync(
			new MailChimpProvider { Name = "mailchimp", ApiKey = "key" },
			"ErtisAuth",
			"no-reply@example.com",
			Recipients,
			"Welcome",
			"<p>Hello</p>",
			TestContext.Current.CancellationToken));
		
		Assert.Equal("provider", exception.ParamName);
		Assert.Contains(nameof(SmtpServerProvider), exception.Message);
	}
	
	[Fact]
	public async Task TemplateMailService_WithAnotherProviderType_ThrowsArgumentExceptionWithoutRequest()
	{
		var restHandler = Substitute.For<IRestHandler>();
		ITemplateMailService service = new MailChimpService(restHandler);
		
		var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SendMailWithTemplateAsync(
			new SmtpServerProvider { Name = "smtp", Host = "smtp.example.com", Port = 587, Username = "mailer", Password = "secret" },
			"ErtisAuth",
			"no-reply@example.com",
			Recipients,
			"Welcome",
			"welcome-template",
			new Dictionary<string, string>(),
			TestContext.Current.CancellationToken));
		
		Assert.Equal("provider", exception.ParamName);
		Assert.Contains(nameof(MailChimpProvider), exception.Message);
		Assert.Empty(restHandler.ReceivedCalls());
	}
	
	#endregion
}
