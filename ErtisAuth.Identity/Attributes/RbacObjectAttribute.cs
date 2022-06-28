using System;

namespace ErtisAuth.Identity.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
	public class RbacObjectAttribute : RbacAttribute
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
}