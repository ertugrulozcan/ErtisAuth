using System;

namespace ErtisAuth.Identity.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
	public class RbacSubjectAttribute : RbacAttribute
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
}