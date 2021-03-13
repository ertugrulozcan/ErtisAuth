using System;
using System.Diagnostics.CodeAnalysis;

namespace ErtisAuth.Core.Models.Roles
{
	public class Rbac : IEquatable<Rbac>
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
			switch (segments.Length)
			{
				case 1:
					return new Rbac
					{
						Subject = RbacSegment.All,
						Resource = (RbacSegment) segments[0],
						Action = RbacSegment.All,
						Object = RbacSegment.All
					};
				case 2:
					return new Rbac
					{
						Subject = RbacSegment.All,
						Resource = (RbacSegment) segments[0],
						Action = (RbacSegment) segments[1],
						Object = RbacSegment.All
					};
				case 3:
					return new Rbac
					{
						Subject = RbacSegment.All,
						Resource = (RbacSegment) segments[0],
						Action = (RbacSegment) segments[1],
						Object = (RbacSegment) segments[2]
					};
				case 4:
					return new Rbac
					{
						Subject = (RbacSegment) segments[0],
						Resource = (RbacSegment) segments[1],
						Action = (RbacSegment) segments[2],
						Object = (RbacSegment) segments[3]
					};
				default:
					throw new ArgumentOutOfRangeException(nameof(path), "The permission path is not valid. ([subject].[resource].[action].[object])");
			}
		}

		public static bool TryParse(string path, out Rbac rbac)
		{
			try
			{
				rbac = Parse(path);
				return true;
			}
			catch
			{
				rbac = null;
				return false;
			}
		}
		
		public static bool operator ==(Rbac rbac1, Rbac rbac2)
		{
			return AreEquals(rbac1, rbac2);
		}

		public static bool operator !=(Rbac rbac1, Rbac rbac2)
		{
			return !(rbac1 == rbac2);
		}

		public override bool Equals(object other)
		{
			if (other is Rbac rbac)
			{
				return AreEquals(this, rbac);	
			}

			return false;
		}

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			return HashCode.Combine(Subject, Resource, Action, Object);
		}

		public bool Equals(Rbac other)
		{
			return AreEquals(this, other);
		}

		private static bool AreEquals(Rbac rbac1, Rbac rbac2)
		{
			if (rbac1 is null || rbac2 is null)
				return false;
			
			return rbac1.GetHashCode() == rbac2.GetHashCode();
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
			return action switch
			{
				CrudActions.Create => CrudActionSegments.Create,
				CrudActions.Read => CrudActionSegments.Read,
				CrudActions.Update => CrudActionSegments.Update,
				CrudActions.Delete => CrudActionSegments.Delete,
				_ => new RbacSegment(action.ToString())
			};
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