namespace ErtisAuth.Extensions.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RbacObjectAttribute : RbacAttribute
{
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="objectName"></param>
	public RbacObjectAttribute(string objectName) : base(objectName)
	{
		
	}
	
	#endregion
}