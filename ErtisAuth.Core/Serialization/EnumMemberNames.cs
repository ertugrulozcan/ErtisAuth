using System.Reflection;
using System.Runtime.Serialization;

namespace ErtisAuth.Core.Serialization;

/// <summary>
/// Maps enum values to their [EnumMember] value (the enum name when the attribute is missing).
/// </summary>
internal static class EnumMemberNames<TEnum> where TEnum : struct, Enum
{
	#region Fields
	
	private static readonly Dictionary<TEnum, string> Names = Enum.GetValues<TEnum>().ToDictionary(
		x => x,
		x => typeof(TEnum).GetField(x.ToString())?.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? x.ToString());
	
	#endregion
	
	#region Methods
	
	public static string GetName(TEnum value)
	{
		return Names.TryGetValue(value, out var name) ? name : value.ToString();
	}
	
	/// <summary>
	/// Accepts the [EnumMember] value and the enum name, case-insensitively ("active", "Active", "ACTIVE").
	/// </summary>
	public static bool TryParse(string? text, out TEnum value)
	{
		foreach (var pair in Names)
		{
			if (string.Equals(pair.Value, text, StringComparison.OrdinalIgnoreCase) || string.Equals(pair.Key.ToString(), text, StringComparison.OrdinalIgnoreCase))
			{
				value = pair.Key;
				return true;
			}
		}
		
		value = default;
		return false;
	}
	
	#endregion
}