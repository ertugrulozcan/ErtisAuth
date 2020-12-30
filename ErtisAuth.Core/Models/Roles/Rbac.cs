using System;

namespace ErtisAuth.Core.Models.Roles
{
	public class Rbac
	{
		#region Properties

		public RbacSegment Subject { get; private set; } = RbacSegment.All;

		public RbacSegment Resource { get; private set; } = RbacSegment.All;

		public RbacSegment Action { get; private set; } = RbacSegment.All;

		public RbacSegment Object { get; private set; } = RbacSegment.All;

		#endregion

		#region Constructors

		/// <summary>
		/// Default Constructor
		/// </summary>
		private Rbac()
		{ }
		
		/// <summary>
		/// Constructor 1
		/// </summary>
		/// <param name="subject"></param>
		/// <param name="resource"></param>
		/// <param name="action"></param>
		/// <param name="obj"></param>
		public Rbac(RbacSegment subject, RbacSegment resource, RbacSegment action, RbacSegment obj)
		{
			this.Subject = subject;
			this.Resource = resource;
			this.Action = action;
			this.Object = obj;
		}

		#endregion

		#region Methods

		public static Rbac Parse(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("Role permission path is empty!");
			}

			var segments = path.Split(RbacSegment.SEPERATOR);
			if (segments.Length < 3)
			{
				throw new ArgumentOutOfRangeException(nameof(path), "The permission path should be contains subject, resource and action segments! (The object segment is optionally, if blank it's mean is all objects are permitted)");
			}

			return new Rbac
			{
				Subject = (RbacSegment) segments[0],
				Resource = (RbacSegment) segments[1],
				Action = (RbacSegment) segments[2],
				Object = segments.Length > 3 ? (RbacSegment) segments[3] : RbacSegment.All
			};
		}

		public static bool TryParse(string path, out Rbac rbac)
		{
			try
			{
				rbac = Parse(path);
				return true;
			}
			catch (Exception e)
			{
				rbac = null;
				return false;
			}
		}
		
		public override string ToString()
		{
			return $"{this.Subject}{RbacSegment.SEPERATOR}{this.Resource}{RbacSegment.SEPERATOR}{this.Action}{RbacSegment.SEPERATOR}{this.Object}";
		}

		#endregion
		
		#region Helper Classes

		public static class ReservedRoles
		{
			public const string Administrator = "admin";
		}

		public static RbacSegment GetSegment(CrudActions action)
		{
			switch (action)
			{
				case CrudActions.Create:
					return CrudActionSegments.Create;
				case CrudActions.Read:
					return CrudActionSegments.Read;
				case CrudActions.Update:
					return CrudActionSegments.Update;
				case CrudActions.Delete:
					return CrudActionSegments.Delete;
				default:
					return new RbacSegment(action.ToString());
			}
		}

		public static class CrudActionSegments
		{
			public static readonly RbacSegment Create = new RbacSegment("create");
			
			public static readonly RbacSegment Read = new RbacSegment("read");
			
			public static readonly RbacSegment Update = new RbacSegment("update");
			
			public static readonly RbacSegment Delete = new RbacSegment("delete");
		}
		
		public enum CrudActions
		{
			Create,
			Read,
			Update,
			Delete
		}

		#endregion
	}
}