using Ertis.Core.Models;
using Ertis.Net.Http;
using Ertis.Net.Rest;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Sdk.Services;

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
	
	public async Task<IResponseResult<TResult>> ExecuteRequestAsync<TResult>(
		HttpMethod method,
		string url,
		IHeaderCollection? headers = null,
		IRequestBody? body = null, 
		CancellationToken cancellationToken = default)
	{
		return await this.restHandler.ExecuteRequestAsync<TResult>(method, url, headers, body, cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	protected async Task<IResponseResult<TResult>> ExecuteRequestAsync<TResult>(
		HttpMethod method,
		string baseUrl,
		IQueryString? queryString = null,
		IHeaderCollection? headers = null,
		IRequestBody? body = null, 
		CancellationToken cancellationToken = default)
	{
		return await this.restHandler.ExecuteRequestAsync<TResult>(method, baseUrl, queryString, headers, body, cancellationToken: cancellationToken).ConfigureAwait(false);
	}
    
	public async Task<IResponseResult> ExecuteRequestAsync(
		HttpMethod method,
		string url,
		IHeaderCollection? headers = null,
		IRequestBody? body = null, 
		CancellationToken cancellationToken = default)
	{
		return await this.restHandler.ExecuteRequestAsync(method, url, headers, body, cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	protected async Task<IResponseResult> ExecuteRequestAsync(
		HttpMethod method,
		string baseUrl,
		IQueryString? queryString = null,
		IHeaderCollection? headers = null,
		IRequestBody? body = null, 
		CancellationToken cancellationToken = default)
	{
		return await this.restHandler.ExecuteRequestAsync(method, baseUrl, queryString, headers, body, cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	#endregion
}