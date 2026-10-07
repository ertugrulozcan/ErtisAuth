namespace ErtisAuth.Core.Models.Mailing;

/// <summary>
/// A hook mail waiting in the mail queue; the payload is a snapshot of the template data taken when the mail is queued
/// </summary>
public sealed record HookMail(MailHook MailHook, string UserId, string MembershipId, object? Payload);
