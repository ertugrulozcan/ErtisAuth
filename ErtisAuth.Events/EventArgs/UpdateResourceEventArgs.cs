using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Events.EventArgs
{
	public class UpdateResourceEventArgs<TModel> : System.EventArgs
	{
		#region Properties

		public Utilizer Utilizer { get; }
		
		public TModel Prior { get; }
		
		public TModel Updated { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="prior"></param>
		/// <param name="updated"></param>
		public UpdateResourceEventArgs(TModel prior, TModel updated)
		{
			this.Prior = prior;
			this.Updated = updated;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="utilizer"></param>
		/// <param name="prior"></param>
		/// <param name="updated"></param>
		public UpdateResourceEventArgs(Utilizer utilizer, TModel prior, TModel updated)
		{
			this.Utilizer = utilizer;
			this.Prior = prior;
			this.Updated = updated;
		}

		#endregion
	}
}