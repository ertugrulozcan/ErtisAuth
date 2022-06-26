using System;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Identity.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
	public class RbacActionAttribute : RbacAttribute
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="action"></param>
		public RbacActionAttribute(Rbac.CrudActions action) : base(Rbac.GetSegment(action))
		{
			
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="customAction"></param>
		public RbacActionAttribute(string customAction) : base(customAction)
		{
			
		}

		#endregion
	}
}