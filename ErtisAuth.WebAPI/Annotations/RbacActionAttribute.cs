using System;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.WebAPI.Annotations
{
	[AttributeUsage(AttributeTargets.Method)]
	public class RbacActionAttribute : Attribute
	{
		#region Properties

		public RbacSegment ActionSegment { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="action"></param>
		public RbacActionAttribute(Rbac.CrudActions action)
		{
			this.ActionSegment = Rbac.GetSegment(action);
		}

		#endregion
	}
}