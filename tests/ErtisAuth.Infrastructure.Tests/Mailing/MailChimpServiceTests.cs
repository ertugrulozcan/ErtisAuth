using System.Net;
using Ertis.Core.Models;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Extensions.Mailing.MailChimp;
using ErtisAuth.Extensions.Mailing.Services;
using ErtisAuth.Extensions.Mailing.Services.Interfaces;
using NSubstitute;
using Recipient = ErtisAuth.Core.Models.Mailing.Recipient;

namespace ErtisAuth.Infrastructure.Tests.Mailing;

/// <summary>
/// MailChimp (Mandrill) answers a failed send with an error response instead of an exception;
/// the failure must be thrown, so the hook mail is reported as failed instead of sent.
/// </summary>
public class MailChimpServiceTests
{
	#region Fields
	
	private readonly IRestHandler _restHandler = Substitute.For<IRestHandler>();
	
	#endregion
	
	#region Helpers
	
	private void RespondWith(IResponseResult response)
	{
		this._restHandler
			.ExecuteRequestAsync(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<IHeaderCollection?>(), Arg.Any<IRequestBody?>(), Arg.Any<CancellationToken>())
			.Returns(response);
	}
	
	/// <summary>
	/// Sends through the non-generic interface, like MailHookService does
	/// </summary>
	private Task SendAsync()
	{
		ITemplateMailService service = new MailChimpService(this._restHandler);
		return service.SendMailWithTemplateAsync(
			new MailChimpProvider { Name = "mailchimp", ApiKey = "key" },
			"ErtisAuth",
			"no-reply@example.com",
			[new Recipient { DisplayName = "John Doe", EmailAddress = "john.doe@example.com" }],
			"Welcome",
			"welcome-template",
			new Dictionary<string, string> { ["name"] = "John" },
			TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Methods
	
	[Fact]
	public async Task SendMailWithTemplateAsync_WithSuccessResponse_SendsTheTemplateToMandrill()
	{
		this.RespondWith(new ResponseResult(HttpStatusCode.OK));
		
		await this.SendAsync();
		
		await this._restHandler.Received(1).ExecuteRequestAsync(
			HttpMethod.Post,
			"https://mandrillapp.com/api/1.0/messages/send-template",
			Arg.Any<IHeaderCollection?>(),
			Arg.Any<IRequestBody?>(),
			Arg.Any<CancellationToken>());
	}
	
	[Fact]
	public async Task SendMailWithTemplateAsync_WithErrorResponse_Throws()
	{
		this.RespondWith(new ResponseResult(HttpStatusCode.InternalServerError, """{"status":"error","name":"Invalid_Key","message":"Invalid API key"}"""));
		
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(this.SendAsync);
		
		Assert.Contains("InternalServerError", exception.Message);
		Assert.Contains("Invalid API key", exception.Message);
	}
	
	#endregion
}
