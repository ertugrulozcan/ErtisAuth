namespace ErtisAuth.Extensions.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RbacSubjectAttribute : RbacAttribute
{
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="subject"></param>
	public RbacSubjectAttribute(string subject) : base(subject)
	{
		
	}
	
	#endregion
}