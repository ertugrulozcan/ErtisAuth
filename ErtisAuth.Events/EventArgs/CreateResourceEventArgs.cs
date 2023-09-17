using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Events.EventArgs
{
	public class CreateResourceEventArgs<TModel> : System.EventArgs
	{
		#region Properties
		
		public Utilizer Utilizer { get; }

		public TModel Resource { get; }
		
		public string MembershipId { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resource"></param>
		public CreateResourceEventArgs(TModel resource)
		{
			this.Resource = resource;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="membershipId"></param>
		public CreateResourceEventArgs(TModel resource, string membershipId)
		{
			this.Resource = resource;
			this.MembershipId = membershipId;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="utilizer"></param>
		/// <param name="resource"></param>
		/// <param name="membershipId"></param>
		public CreateResourceEventArgs(Utilizer utilizer, TModel resource, string membershipId)
		{
			this.Utilizer = utilizer;
			this.Resource = resource;
			this.MembershipId = membershipId;
		}

		#endregion
	}
}