using System.Runtime.Serialization;

namespace ErtisAuth.Core.Models;

public enum Status
{
	[EnumMember(Value = "passive")]
	Passive,
		
	[EnumMember(Value = "active")]
	Active
}