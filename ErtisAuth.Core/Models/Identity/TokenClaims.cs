using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Core.Models.Identity
{
	public class TokenClaims
	{
		#region Properties

		public string TokenId { get; }
		
		public string Issuer { get; }

		public string Audience { get; }
		
		public TimeSpan ExpiresIn { get; }
		
		public string Subject { get; }
		
		public string Principal { get; }
		
		public string SecretKey { get; }
		
		public string FirstName { get; }
		
		public string LastName { get; }
		
		public string Username { get; }
		
		public string EmailAddress { get; }
		
		public string Scope { get; init; }
		
		public ReadOnlyDictionary<string, object> AdditionalClaims { get; }
		
		private Dictionary<string, object> OtherClaims { get; }
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tokenId"></param>
		/// <param name="user"></param>
		/// <param name="membership"></param>
		/// <param name="expiresIn"></param>
		public TokenClaims(string tokenId, User user, Membership membership, TimeSpan? expiresIn = null)
		{
			this.SecretKey = membership.SecretKey;
			this.Issuer = membership.Name;
			this.Audience = user.Id;
			this.Subject = user.Id;
			this.TokenId = tokenId;
			this.Principal = membership.Id;
			this.ExpiresIn = expiresIn ?? TimeSpan.FromSeconds(membership.ExpiresIn);
			this.FirstName = user.FirstName;
			this.LastName = user.LastName;
			this.Username = user.Username;
			this.EmailAddress = user.EmailAddress;
			
			this.OtherClaims = new Dictionary<string, object>();
			this.AdditionalClaims = new ReadOnlyDictionary<string, object>(this.OtherClaims);
		}

		#endregion

		#region Methods

		public TokenClaims AddClaim<T>(string name, T value)
		{
			this.OtherClaims.Add(name, value);
			return this;
		}
		
		public TokenClaims RemoveClaim(string name)
		{
            this.OtherClaims.Remove(name);
            return this;
		}

		#endregion
	}
}