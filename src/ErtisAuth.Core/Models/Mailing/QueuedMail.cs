namespace ErtisAuth.Core.Models.Mailing;

/// <summary>
/// A mail waiting in the mail queue; OnCompleted is called after the sending, with the error if the mail could not be sent
/// </summary>
public sealed record QueuedMail(MailMessage Mail, Func<Exception?, Task>? OnCompleted);
