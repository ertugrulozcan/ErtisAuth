namespace ErtisAuth.Extensions.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RbacResourceAttribute : RbacAttribute
{
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="resourceName"></param>
	public RbacResourceAttribute(string resourceName) : base(resourceName)
	{
		
	}
	
	#endregion
}