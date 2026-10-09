namespace ErtisAuth.Core.Extensions;

public static class ValidationExtensions
{
	#region Methods
	
	public static bool IsValidSlug(this string slug, out string? error)
	{
		if (string.IsNullOrEmpty(slug))
		{
			error = "The slug is required";
			return false;
		}
		
		if (slug.Contains(' '))
		{
			error = "The slug can not contain whitespace";
			return false;
		}
		
		if (char.IsDigit(slug[0]))
		{
			error = "The slug can not start with a digit";
			return false;
		}
		
		error = null;
		return true;
	}
	
	#endregion
}