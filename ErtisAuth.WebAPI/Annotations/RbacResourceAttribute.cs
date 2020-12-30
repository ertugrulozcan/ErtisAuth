using System;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.WebAPI.Annotations
{
	[AttributeUsage(AttributeTargets.Class)]
	public class RbacResourceAttribute : Attribute
	{
		#region Properties

		public RbacSegment ResourceSegment { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resourceName"></param>
		public RbacResourceAttribute(string resourceName)
		{
			this.ResourceSegment = new RbacSegment(resourceName);
		}

		#endregion
	}
}