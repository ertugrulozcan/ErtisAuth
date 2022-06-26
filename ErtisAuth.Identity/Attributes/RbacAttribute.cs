using System;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Identity.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
	public abstract class RbacAttribute : Attribute
	{
		#region Properties

		public RbacSegment Value { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="segmentValue"></param>
		protected RbacAttribute(string segmentValue)
		{
			this.Value = new RbacSegment(segmentValue);
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="segment"></param>
		protected RbacAttribute(RbacSegment segment)
		{
			this.Value = segment;
		}

		#endregion
	}
}