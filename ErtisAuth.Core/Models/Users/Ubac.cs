using System;
using System.Diagnostics.CodeAnalysis;
using UbacSegment = ErtisAuth.Core.Models.Roles.RbacSegment;

namespace ErtisAuth.Core.Models.Users
{
	public class Ubac : IEquatable<Ubac>
	{
		#region Properties

		public UbacSegment Resource { get; private set; } = UbacSegment.All;

		public UbacSegment Action { get; private set; } = UbacSegment.All;

		public UbacSegment Object { get; private set; } = UbacSegment.All;

		#endregion

		#region Constructors

		/// <summary>
		/// Default Constructor
		/// </summary>
		private Ubac()
		{ }
		
		/// <summary>
		/// Constructor 1
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="action"></param>
		/// <param name="obj"></param>
		public Ubac(UbacSegment resource, UbacSegment action, UbacSegment obj)
		{
			this.Resource = resource;
			this.Action = action;
			this.Object = obj;
		}

		#endregion

		#region Methods

		public static Ubac Parse(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("Role permission path is empty!");
			}
			
			var segments = path.Split(UbacSegment.SEPARATOR);
			switch (segments.Length)
			{
				case 1:
					return new Ubac
					{
						Resource = (UbacSegment) segments[0],
						Action = UbacSegment.All,
						Object = UbacSegment.All
					};
				case 2:
					return new Ubac
					{
						Resource = (UbacSegment) segments[0],
						Action = (UbacSegment) segments[1],
						Object = UbacSegment.All
					};
				case 3:
					return new Ubac
					{
						Resource = (UbacSegment) segments[0],
						Action = (UbacSegment) segments[1],
						Object = (UbacSegment) segments[2]
					};
				default:
					throw new ArgumentOutOfRangeException(nameof(path), "The permission path is not valid. ([subject].[resource].[action].[object])");
			}
		}

		public static bool TryParse(string path, out Ubac ubac)
		{
			try
			{
				ubac = Parse(path);
				return true;
			}
			catch
			{
				ubac = null;
				return false;
			}
		}
		
		public static bool operator ==(Ubac ubac1, Ubac ubac2)
		{
			return AreEquals(ubac1, ubac2);
		}

		public static bool operator !=(Ubac ubac1, Ubac ubac2)
		{
			return !(ubac1 == ubac2);
		}

		public override bool Equals(object other)
		{
			if (other is Ubac ubac)
			{
				return AreEquals(this, ubac);	
			}

			return false;
		}

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			return HashCode.Combine(Resource, Action, Object);
		}

		public bool Equals(Ubac other)
		{
			return AreEquals(this, other);
		}

		private static bool AreEquals(Ubac ubac1, Ubac ubac2)
		{
			if (ubac1 is null && ubac2 is null)
				return true;
			
			if (ubac1 is null || ubac2 is null)
				return false;
			
			return ubac1.GetHashCode() == ubac2.GetHashCode();
		}
		
		public override string ToString()
		{
			return $"{this.Resource}{UbacSegment.SEPARATOR}{this.Action}{UbacSegment.SEPARATOR}{this.Object}";
		}

		#endregion
	}
}