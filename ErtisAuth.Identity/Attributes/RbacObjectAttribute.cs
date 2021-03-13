using System;

namespace ErtisAuth.Identity.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
	public class RbacObjectAttribute : Attribute
	{
		#region Properties

		public string RouteParameterName { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="routeParameter"></param>
		public RbacObjectAttribute(string routeParameter)
		{
			this.RouteParameterName = routeParameter;
		}

		#endregion
	}
}