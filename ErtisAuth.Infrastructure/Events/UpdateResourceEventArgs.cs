using System;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Infrastructure.Events
{
	public class UpdateResourceEventArgs<TModel> : EventArgs
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