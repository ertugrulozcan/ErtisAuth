using System.Text.Json.Nodes;

namespace ErtisAuth.Core.Models.Webhooks;

/// <summary>
/// A webhook call waiting in the webhook queue; the event data ({ document, prior }) is a JSON snapshot taken when the call is queued
/// </summary>
public sealed record WebhookCall(Webhook Webhook, string UtilizerId, string MembershipId, JsonNode? EventData);
