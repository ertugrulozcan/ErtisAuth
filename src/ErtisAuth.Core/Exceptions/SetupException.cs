using System.Text.Json.Serialization;
using Ertis.Core.Exceptions;
using Ertis.Schema.Exceptions;

namespace ErtisAuth.Core.Exceptions;

public class SetupException : Exception
{
	#region Properties
	
	[JsonPropertyName("resource")]
	public string Resource { get; }
	
	[JsonPropertyName("errors")]
	public IEnumerable<string>? Errors { get; }
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="exception"></param>
	/// <param name="resource"></param>
	public SetupException(Exception exception, string resource) : base(exception.Message, exception)
	{
		this.Resource = resource;
		this.Errors = exception switch
		{
			ValidationException validationException => validationException.Errors,
			CumulativeValidationException cumulativeValidationException => cumulativeValidationException.Errors.Select(x => $"{x.Message} ({x.FieldPath})"),
			FieldValidationException fieldValidationException => new[]
			{
				$"{fieldValidationException.Message} ({fieldValidationException.FieldPath})"
			},
			_ => null
		};
	}
	
	#endregion
}