using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Roles;
using UbacSegment = ErtisAuth.Core.Models.Roles.RbacSegment;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Core.Models.Users;

public class Ubac : IEquatable<Ubac>
{
	#region Properties
	
	public UbacSegment Resource { get; private init; } = UbacSegment.All;
	
	public UbacSegment Action { get; private init; } = UbacSegment.All;
	
	public UbacSegment Object { get; private init; } = UbacSegment.All;
	
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
			throw ErtisAuthException.InvalidUbac("The ubac expression can not be empty");
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
				throw ErtisAuthException.InvalidUbac();
		}
	}
	
	public static bool TryParse(string path, out Ubac? ubac)
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
	
	public static bool operator ==(Ubac? ubac1, Ubac? ubac2)
	{
		return AreEquals(ubac1, ubac2);
	}
	
	public static bool operator !=(Ubac? ubac1, Ubac? ubac2)
	{
		return !(ubac1 == ubac2);
	}
	
	public override bool Equals(object? other)
	{
		if (other is Ubac ubac)
		{
			return AreEquals(this, ubac);	
		}
		
		return false;
	}
	
	public override int GetHashCode()
	{
		return HashCode.Combine(
			this.Resource.GetHashCode(Rbac.NameSegmentComparison),
			this.Action.GetHashCode(Rbac.NameSegmentComparison),
			this.Object);
	}
	
	public bool Equals(Ubac? other)
	{
		return AreEquals(this, other);
	}
	
	private static bool AreEquals(Ubac? ubac1, Ubac? ubac2)
	{
		if (ubac1 is null && ubac2 is null)
		{
			return true;
		}
		
		if (ubac1 is null || ubac2 is null)
		{
			return false;
		}
		
		// Same rules as permission matching
		return
			ubac1.Resource.Equals(ubac2.Resource, Rbac.NameSegmentComparison) &&
			ubac1.Action.Equals(ubac2.Action, Rbac.NameSegmentComparison) &&
			ubac1.Object.Equals(ubac2.Object);
	}
	
	public override string ToString()
	{
		return $"{this.Resource}{UbacSegment.SEPARATOR}{this.Action}{UbacSegment.SEPARATOR}{this.Object}";
	}
	
	#endregion
}