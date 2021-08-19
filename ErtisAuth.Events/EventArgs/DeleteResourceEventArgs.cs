using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Events.EventArgs
{
	public class DeleteResourceEventArgs<TModel> : System.EventArgs
	{
		#region Properties

		public Utilizer Utilizer { get; }

		public TModel Resource { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resource"></param>
		public DeleteResourceEventArgs(TModel resource)
		{
			this.Resource = resource;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="utilizer"></param>
		/// <param name="resource"></param>
		public DeleteResourceEventArgs(Utilizer utilizer, TModel resource)
		{
			this.Utilizer = utilizer;
			this.Resource = resource;
		}

		#endregion
	}
}