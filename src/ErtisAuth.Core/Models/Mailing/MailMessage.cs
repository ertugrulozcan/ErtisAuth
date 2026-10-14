namespace ErtisAuth.Core.Models.Mailing;

/// <summary>
/// A rendered mail, ready to be sent with a mail provider of the membership.
/// The provider's delivery mode decides which content is sent: the html body (raw) or the template with its arguments (template).
/// </summary>
public sealed class MailMessage
{
	#region Properties
	
	public required string MembershipId { get; init; }
	
	/// <summary>
	/// The slug of the membership's mail provider
	/// </summary>
	public required string MailProvider { get; init; }
	
	public required string FromName { get; init; }
	
	public required string FromAddress { get; init; }
	
	public required IReadOnlyList<Recipient> Recipients { get; init; }
	
	public required string Subject { get; init; }
	
	public string? HtmlBody { get; init; }
	
	public string? TemplateId { get; init; }
	
	public IDictionary<string, string>? TemplateArguments { get; init; }
	
	#endregion
}
