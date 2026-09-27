using Microsoft.Extensions.Options;

namespace ErtisAuth.Sdk.Configuration;

/// <summary>
/// Validates the SDK options at startup, so that a missing or wrong setting fails with a clear message
/// instead of an obscure error on the first request.
/// </summary>
public static class ErtisAuthOptionsValidator
{
	#region Methods
	
	public static IReadOnlyList<string> GetErrors(ErtisAuthOptions options)
	{
		var errors = new List<string>();
		
		if (string.IsNullOrWhiteSpace(options.BaseUrl))
		{
			errors.Add($"{nameof(ErtisAuthOptions.BaseUrl)} is required");
		}
		else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
		{
			errors.Add($"{nameof(ErtisAuthOptions.BaseUrl)} must be an absolute http or https url ('{options.BaseUrl}')");
		}
		
		if (string.IsNullOrWhiteSpace(options.MembershipId))
		{
			errors.Add($"{nameof(ErtisAuthOptions.MembershipId)} is required");
		}
		
		if (options.BasicTokenCacheTTL is < 0)
		{
			errors.Add($"{nameof(ErtisAuthOptions.BasicTokenCacheTTL)} can not be negative ({options.BasicTokenCacheTTL})");
		}
		
		return errors;
	}
	
	/// <summary>
	/// Throws an <see cref="OptionsValidationException"/> listing every invalid setting.
	/// </summary>
	/// <param name="options"></param>
	/// <param name="source">Where the options come from, shown in the error message (e.g. the configuration section)</param>
	public static void Validate(ErtisAuthOptions options, string source)
	{
		var errors = GetErrors(options);
		if (errors.Count > 0)
		{
			throw new OptionsValidationException(
				source,
				typeof(ErtisAuthOptions),
				errors.Select(x => $"ErtisAuth SDK configuration is invalid ({source}): {x}"));
		}
	}
	
	#endregion
}
