using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Mailing;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.MailHooks;

/// <summary>
/// The create request of a mail hook; the id and the membership come from the route.
/// </summary>
public class CreateMailHookFormModel
{
	#region Properties
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonPropertyName("description")]
	public string? Description { get; set; }
	
	[JsonPropertyName("event")]
	public string? Event { get; set; }
	
	[JsonPropertyName("status")]
	public string? Status { get; set; }
	
	[JsonPropertyName("mailSubject")]
	public string? MailSubject { get; set; }
	
	[JsonPropertyName("mailTemplate")]
	public string? MailTemplate { get; set; }
	
	[JsonPropertyName("fromName")]
	public string? FromName { get; set; }
	
	[JsonPropertyName("fromAddress")]
	public string? FromAddress { get; set; }
	
	[JsonPropertyName("sendToUtilizer")]
	public bool SendToUtilizer { get; set; }
	
	[JsonPropertyName("recipients")]
	public Recipient[]? Recipients { get; set; }
	
	[JsonPropertyName("mailProvider")]
	public string? MailProvider { get; set; }
	
	[JsonPropertyName("variables")]
	public MailHookVariable[]? Variables { get; set; }
	
	#endregion
	
	internal MailHook ToMailHook(string membershipId)
	{
		var mailHook = new MailHook
		{
			Name = this.Name ?? string.Empty,
			Description = this.Description,
			Event = this.Event,
			Status = this.Status,
			MailSubject = this.MailSubject,
			MailTemplate = this.MailTemplate,
			FromName = this.FromName,
			FromAddress = this.FromAddress,
			SendToUtilizer = this.SendToUtilizer,
			Recipients = this.Recipients,
			MailProvider = this.MailProvider,
			Variables = this.Variables,
			MembershipId = membershipId
		};
		
		// Otherwise derived from the name
		if (!string.IsNullOrEmpty(this.Slug))
		{
			mailHook.Slug = this.Slug;
		}
		
		return mailHook;
	}
}