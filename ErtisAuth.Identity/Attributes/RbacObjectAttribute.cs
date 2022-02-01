using System;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Identity.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
	public class RbacObjectAttribute : Attribute
	{
		#region Properties

		public RbacSegment ObjectSegment { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="objectName"></param>
		public RbacObjectAttribute(string objectName)
		{
			this.ObjectSegment = new RbacSegment(objectName);
		}

		#endregion
	}
}