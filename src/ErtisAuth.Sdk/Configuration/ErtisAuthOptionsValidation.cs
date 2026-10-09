using Microsoft.Extensions.Options;

namespace ErtisAuth.Sdk.Configuration;

/// <summary>
/// Validates the options bound from the configuration of the host, when the host starts (ValidateOnStart) or at the first use.
/// </summary>
internal sealed class ErtisAuthOptionsValidation(string source) : IValidateOptions<ErtisAuthOptions>
{
	#region Methods
	
	public ValidateOptionsResult Validate(string? name, ErtisAuthOptions options)
	{
		if (name != Options.DefaultName)
		{
			return ValidateOptionsResult.Skip;
		}
		
		var errors = ErtisAuthOptionsValidator.GetErrors(options);
		return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors.Select(x => ErtisAuthOptionsValidator.FormatError(source, x)));
	}
	
	#endregion
}
