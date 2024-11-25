using System;

namespace ErtisAuth.Identity.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class RbacResourceAttribute : RbacAttribute
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
}