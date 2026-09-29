using System.Net;

namespace ErtisAuth.Core.Exceptions;

/// <summary>
/// A write rejected by a unique index of the database.
/// </summary>
public class DuplicateKeyException : ErtisAuthException
{
	#region Properties
	
	/// <summary>
	/// The name of the violated index, when the database error names it.
	/// </summary>
	public string? IndexName { get; init; }
	
	#endregion
	
	#region Constructors
	
	internal DuplicateKeyException(HttpStatusCode statusCode, string message, string errorCode) : base(statusCode, message, errorCode)
	{
	
	}
	
	#endregion
}
