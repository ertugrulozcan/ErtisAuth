using System.Runtime.Serialization;

namespace ErtisAuth.Core.Models.Webhooks
{
	public enum WebhookStatus
	{
		[EnumMember(Value = "passive")]
		Passive,
		
		[EnumMember(Value = "active")]
		Active
	}
}