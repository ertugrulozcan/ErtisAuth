using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Extensions.Mailkit.Serialization;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;
using Newtonsoft.Json;

namespace ErtisAuth.Sdk.Services
{
	public class MembershipService : BaseRestService, IMembershipService
	{
		#region Properties

		// ReSharper disable once MemberCanBePrivate.Global
		protected string AuthApiBaseUrl { get; }
		
		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public MembershipService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(restHandler)
		{
			this.AuthApiBaseUrl = ertisAuthOptions.BaseUrl;
		}

		#endregion

		#region Create Methods

		public IResponseResult<Membership> CreateMembership(Membership membership, TokenBase token) =>
			this.CreateMembershipAsync(membership, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Membership>> CreateMembershipAsync(Membership membership, TokenBase token, CancellationToken cancellationToken = default)
		{
			return await this.ExecuteRequestAsync<Membership>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(membership),
				cancellationToken: cancellationToken);
		}

		#endregion
		
		#region Read Methods
		
		public IResponseResult<Membership> GetMembership(string membershipId, TokenBase token) =>
			this.GetMembershipAsync(membershipId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Membership>> GetMembershipAsync(string membershipId, TokenBase token, CancellationToken cancellationToken = default)
		{
			return await this.ExecuteRequestAsync<Membership>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{membershipId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				converters: new JsonConverter[] { new MailProviderJsonConverter() }, 
				cancellationToken: cancellationToken);
		}
		
		public IResponseResult<IPaginationCollection<Membership>> GetMemberships(
			TokenBase token, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null, 
			string searchKeyword = null) =>
			this.GetMembershipsAsync(
				token,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection,
				searchKeyword)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<Membership>>> GetMembershipsAsync(
			TokenBase token,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null,
			string searchKeyword = null, 
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(searchKeyword) || string.IsNullOrEmpty(searchKeyword.Trim()))
			{
				return await this.ExecuteRequestAsync<PaginationCollection<Membership>>(
					HttpMethod.Get, 
					$"{this.AuthApiBaseUrl}/memberships", 
					QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
					HeaderCollection.Add("Authorization", token.ToString()),
					cancellationToken: cancellationToken);	
			}
			else
			{
				return await this.ExecuteRequestAsync<PaginationCollection<Membership>>(
					HttpMethod.Get, 
					$"{this.AuthApiBaseUrl}/memberships/search", 
					QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection).Add("keyword", searchKeyword), 
					HeaderCollection.Add("Authorization", token.ToString()),
					cancellationToken: cancellationToken);
			}
		}
		
		#endregion
		
		#region Query Methods
		
		public IResponseResult<IPaginationCollection<Membership>> QueryMemberships(
			TokenBase token, 
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null) =>
			this.QueryMembershipsAsync(
				token,
				query,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<Membership>>> QueryMembershipsAsync(
			TokenBase token,
			string query,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null, 
			CancellationToken cancellationToken = default)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Membership>>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/_query", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(JsonConvert.DeserializeObject(query) ?? "{}"),
				cancellationToken: cancellationToken);
		}
		
		#endregion
		
		#region Update Methods
		
		public IResponseResult<Membership> UpdateMembership(Membership membership, TokenBase token) =>
			this.UpdateMembershipAsync(membership, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Membership>> UpdateMembershipAsync(Membership membership, TokenBase token, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(membership.Id))
			{
				return new ResponseResult<Membership>(false, "Membership id is required!");
			}
			
			return await this.ExecuteRequestAsync<Membership>(
				HttpMethod.Put, 
				$"{this.AuthApiBaseUrl}/memberships/{membership.Id}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(membership),
				cancellationToken: cancellationToken);
		}
		
		#endregion
		
		#region Delete Methods
		
		public IResponseResult DeleteMembership(string membershipId, TokenBase token) =>
			this.DeleteMembershipAsync(membershipId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult> DeleteMembershipAsync(string membershipId, TokenBase token, CancellationToken cancellationToken = default)
		{
			return await this.ExecuteRequestAsync<Membership>(
				HttpMethod.Delete, 
				$"{this.AuthApiBaseUrl}/memberships/{membershipId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				cancellationToken: cancellationToken);
		}
		
		#endregion
	}
}