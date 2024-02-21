using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using Newtonsoft.Json;

namespace ErtisAuth.Sdk.Services
{
	public abstract class BaseRestService
	{
		#region Services

		private readonly IRestHandler restHandler;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="restHandler"></param>
		protected BaseRestService(IRestHandler restHandler)
		{
			this.restHandler = restHandler;
		}

		#endregion

		#region Methods

		public IResponseResult<TResult> ExecuteRequest<TResult>(
			HttpMethod method,
			string url,
			IHeaderCollection headers = null,
			IRequestBody body = null, 
			JsonConverter[] converters = null)
		{
			return this.restHandler.ExecuteRequest<TResult>(method, url, headers, body, converters: converters);
		}
        
		public async Task<IResponseResult<TResult>> ExecuteRequestAsync<TResult>(
			HttpMethod method,
			string url,
			IHeaderCollection headers = null,
			IRequestBody body = null, 
			JsonConverter[] converters = null, 
			CancellationToken cancellationToken = default)
		{
			return await this.restHandler.ExecuteRequestAsync<TResult>(method, url, headers, body, converters: converters, cancellationToken: cancellationToken);
		}

		protected IResponseResult<TResult> ExecuteRequest<TResult>(
			HttpMethod method,
			string baseUrl,
			IQueryString queryString = null,
			IHeaderCollection headers = null,
			IRequestBody body = null, 
			JsonConverter[] converters = null)
		{
			return this.restHandler.ExecuteRequest<TResult>(method, baseUrl, queryString, headers, body, converters: converters);
		}

		protected async Task<IResponseResult<TResult>> ExecuteRequestAsync<TResult>(
			HttpMethod method,
			string baseUrl,
			IQueryString queryString = null,
			IHeaderCollection headers = null,
			IRequestBody body = null, 
			JsonConverter[] converters = null, 
			CancellationToken cancellationToken = default)
		{
			return await this.restHandler.ExecuteRequestAsync<TResult>(method, baseUrl, queryString, headers, body, converters: converters, cancellationToken: cancellationToken);
		}
        
		public IResponseResult ExecuteRequest(
			HttpMethod method,
			string url,
			IHeaderCollection headers = null,
			IRequestBody body = null)
		{
			return this.restHandler.ExecuteRequest(method, url, headers, body);
		}
        
		public async Task<IResponseResult> ExecuteRequestAsync(
			HttpMethod method,
			string url,
			IHeaderCollection headers = null,
			IRequestBody body = null, 
			CancellationToken cancellationToken = default)
		{
			return await this.restHandler.ExecuteRequestAsync(method, url, headers, body, cancellationToken: cancellationToken);
		}

		protected IResponseResult ExecuteRequest(
			HttpMethod method,
			string baseUrl,
			IQueryString queryString = null,
			IHeaderCollection headers = null,
			IRequestBody body = null)
		{
			return this.restHandler.ExecuteRequest(method, baseUrl, queryString, headers, body);
		}

		protected async Task<IResponseResult> ExecuteRequestAsync(
			HttpMethod method,
			string baseUrl,
			IQueryString queryString = null,
			IHeaderCollection headers = null,
			IRequestBody body = null, 
			CancellationToken cancellationToken = default)
		{
			return await this.restHandler.ExecuteRequestAsync(method, baseUrl, queryString, headers, body, cancellationToken: cancellationToken);
		}

		#endregion
	}
}