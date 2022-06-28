using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public abstract class MembershipBoundedService : BaseRestService
	{
		#region Properties

		protected string BaseUrl { get; }
		
		protected string MembershipId { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		protected MembershipBoundedService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(restHandler)
		{
			this.BaseUrl = ertisAuthOptions.BaseUrl;
			this.MembershipId = ertisAuthOptions.MembershipId;
		}

		#endregion
	}
	
	public abstract class MembershipBoundedService<T> : ReadonlyMembershipBoundedService<T>, IMembershipBoundedService<T> where T : IHasIdentifier
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		protected MembershipBoundedService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion
		
		#region Create Methods

		public IResponseResult<T> Create<TCreateModel>(TCreateModel model, TokenBase token) where TCreateModel : T =>
			this.CreateAsync(model, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<T>> CreateAsync<TCreateModel>(TCreateModel model, TokenBase token) where TCreateModel : T
		{
			return await this.ExecuteRequestAsync<T>(
				HttpMethod.Post, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(model));
		}

		#endregion
		
		#region Update Methods

		public IResponseResult<T> Update(T model, TokenBase token) =>
			this.UpdateAsync(model, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<T>> UpdateAsync(T model, TokenBase token)
		{
			if (string.IsNullOrEmpty(model.Id))
			{
				return new ResponseResult<T>(false, "Id is required!");
			}
			
			return await this.ExecuteRequestAsync<T>(
				HttpMethod.Put, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/{model.Id}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(model));
		}

		#endregion
		
		#region Delete Methods

		public IResponseResult Delete(string modelId, TokenBase token) =>
			this.DeleteAsync(modelId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult> DeleteAsync(string modelId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<T>(
				HttpMethod.Delete, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/{modelId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		public IResponseResult BulkDelete(IEnumerable<string> modelIds, TokenBase token) =>
			this.BulkDeleteAsync(modelIds, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult> BulkDeleteAsync(IEnumerable<string> modelIds, TokenBase token)
		{
			return await this.ExecuteRequestAsync<T>(
				HttpMethod.Delete, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(modelIds));
		}

		#endregion
	}
}