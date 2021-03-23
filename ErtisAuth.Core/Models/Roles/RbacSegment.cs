using System;

namespace ErtisAuth.Core.Models.Roles
{
	public readonly struct RbacSegment : IEquatable<RbacSegment>, IEquatable<string>
	{
		#region Constants

		public const char SEPERATOR = '.';

		#endregion
		
		#region Statics

		public static readonly RbacSegment All = new RbacSegment("*", "__all__");

		#endregion

		#region Properties
		
		public string Value { get; }
		
		public string Slug { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="ArgumentException"></exception>
		public RbacSegment(string value)
		{
			if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
			{
				throw new ArgumentNullException(nameof(value), "Segment value could not be null!");
			}
			
			if (value == All.Value || value == All.Slug)
			{
				throw new ArgumentException($"'{value}' is a reserved keyword, it's could not be use as segment value.");
			}
			
			this.Value = value;
			this.Slug = value;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value"></param>
		/// <param name="slug"></param>
		private RbacSegment(string value, string slug)
		{
			this.Value = value;
			this.Slug = slug;
		}

		#endregion

		#region Implicit & Explicit Operators

		public static implicit operator string(RbacSegment segment) => segment.Value;
		
		public static explicit operator RbacSegment(string value)
		{
			if (value == All.Value)
			{
				return All;
			}
			
			return new RbacSegment(value);
		}

		#endregion
		
		#region Methods

		public bool IsAll()
		{
			return this.Equals(All);
		}
		
		public override string ToString()
		{
			return this.Value;
		}

		public bool Equals(RbacSegment other)
		{
			return this.AreEqual(other);
		}
		
		public bool Equals(RbacSegment other, StringComparison stringComparison)
		{
			return this.AreEqual(other, stringComparison);
		}

		public bool Equals(string other)
		{
			return this.AreEqual(other);
		}

		public override bool Equals(object obj)
		{
			return this.AreEqual(obj);
		}

		private bool AreEqual(object obj, StringComparison? stringComparison = null)
		{
			if (stringComparison == null)
			{
				if (obj is RbacSegment other)
				{
					return this.Value == other.Value && this.Slug == other.Slug;
				}
				
				return obj switch
				{
					null => false,
					string str => this.Value == str && this.Slug == str,
					_ => false
				};
			}
			else
			{
				if (obj is RbacSegment other)
				{
					return string.Compare(this.Value, other.Value, stringComparison.Value) == 0 && string.Compare(this.Slug, other.Slug, stringComparison.Value) == 0;
				}
				
				return obj switch
				{
					null => false,
					string str => string.Compare(this.Value, str, stringComparison.Value) == 0 && string.Compare(this.Slug, str, stringComparison.Value) == 0,
					_ => false
				};
			}
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.Value, this.Slug);
		}

		#endregion
	}
}