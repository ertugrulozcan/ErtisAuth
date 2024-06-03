using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Sdk.Configuration
{
	public interface IErtisAuthOptions
	{
		#region Properties

		string BaseUrl { get; }
		
		string MembershipId { get; }
		
		int? BasicTokenCacheTTL { get; }

		#endregion
	}
	
	public class ErtisAuthOptions : IErtisAuthOptions
	{
		#region Properties
		
		public string BaseUrl { get; set; }
		
		public string MembershipId { get; set; }

		public int? BasicTokenCacheTTL { get; set; }

		#endregion
	}
	
	public class ScopedErtisAuthOptions : IErtisAuthOptions
	{
		#region Services

		private readonly IHttpContextAccessor httpContextAccessor;

		#endregion
		
		#region Properties

		private string TempBaseUrl { get; set; }
		
		private string TempMembershipId { get; set; }
		
		public string BaseUrl
		{
			get
			{
				if (!string.IsNullOrEmpty(this.TempBaseUrl))
				{
					var tempBaseUrl = this.TempBaseUrl;
					this.TempBaseUrl = null;
					return tempBaseUrl;
				}
				
				var baseUrlClaim = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "server_url");
				if (!(baseUrlClaim == null || string.IsNullOrEmpty(baseUrlClaim.Value)))
				{
					return baseUrlClaim.Value;
				}

				return null;
			}
		}

		public string MembershipId
		{
			get
			{
				if (!string.IsNullOrEmpty(this.TempMembershipId))
				{
					var tempMembershipId = this.TempMembershipId;
					this.TempMembershipId = null;
					return tempMembershipId;
				}
				
				var membershipIdClaim = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "membership_id");
				if (!(membershipIdClaim == null || string.IsNullOrEmpty(membershipIdClaim.Value)))
				{
					return membershipIdClaim.Value;
				}

				return null;
			}
		}
		
		public int? BasicTokenCacheTTL { get; set; }
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="httpContextAccessor"></param>
		/// <exception cref="Exception"></exception>
		public ScopedErtisAuthOptions(IHttpContextAccessor httpContextAccessor)
		{
			this.httpContextAccessor = httpContextAccessor;
			if (this.httpContextAccessor?.HttpContext?.User?.Claims == null)
			{
				throw new Exception("HttpContent could not read!");
			}
		}

		#endregion

		#region Methods

		public void SetTemporary(string baseUrl, string membershipId)
		{
			this.TempBaseUrl = baseUrl;
			this.TempMembershipId = membershipId;
		}

		#endregion
	}
}