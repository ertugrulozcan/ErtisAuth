using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ErtisAuth.Infrastructure.Helpers;

/// <summary>
/// The username and the email address (the login names) are stored normalized, and the values looked up by them are
/// normalized the same way, so that the login and the uniqueness of the login names don't depend on case, surrounding
/// white space or the unicode composition of the characters.
/// </summary>
public static class LoginNameHelper
{
	#region Methods
	
	/// <summary>
	/// Trimmed, unicode NFC composed and lower cased (invariant culture).
	/// </summary>
	[return: NotNullIfNotNull(nameof(value))]
	public static string? Normalize(string? value)
	{
		return value?.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
	}
	
	#endregion
}
