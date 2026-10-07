using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Extensions.Mailing.MailChimp;
using ErtisAuth.Extensions.Mailing.Services.Interfaces;
using RecipientModel = ErtisAuth.Core.Models.Mailing.Recipient;
using MailChimpRecipient = ErtisAuth.Extensions.Mailing.MailChimp.Recipient;

namespace ErtisAuth.Extensions.Mailing.Services;

public class MailChimpService : ITemplateMailService<MailChimpProvider>
{
	#region Services
	
	private readonly IRestHandler _restHandler;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="restHandler"></param>
	public MailChimpService(IRestHandler restHandler)
	{
		this._restHandler = restHandler;
	}
	
	#endregion
	
	#region Methods
	
	public MailProviderType GetProviderType()
	{
		return MailProviderType.MailChimp;
	}
	
	public async Task SendMailWithTemplateAsync(
		MailChimpProvider provider,
		string fromName,
		string fromAddress,
		IEnumerable<RecipientModel> recipients,
		string subject,
		string templateId,
		IDictionary<string, string> arguments,
		CancellationToken cancellationToken = default)
	{
		var response = await this._restHandler.ExecuteRequestAsync(
			HttpMethod.Post,
			"https://mandrillapp.com/api/1.0/messages/send-template",
			HeaderCollection.Empty,
			new JsonRequestBody(new TemplatePayload
			{
				Key = provider.ApiKey,
				TemplateName = templateId,
				TemplateContent = arguments.Select(x => new TemplateContentItem
				{
					Name = x.Key,
					Content = x.Value
				}).ToArray(),
				Message = new Message
				{
					Subject = subject,
					FromEmail = fromAddress,
					FromName = fromName,
					To = recipients.Select(x => new MailChimpRecipient
					{
						Email = x.EmailAddress,
						Name = x.DisplayName,
						Type = RecipientType.to
					}).ToArray(),
					GlobalMergeVars = arguments.Select(x => new Variable
					{
						Name = x.Key,
						Content = x.Value
					}).ToArray()
				},
				Async = true
			}
		), cancellationToken: cancellationToken);
		
		if (!response.IsSuccess)
		{
			throw new InvalidOperationException($"The MailChimp mail could not be sent ({response.StatusCode}): {response.Message}");
		}
	}
	
	#endregion
}
