using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Extensions;
using ErtisAuth.Sdk.Services.Interfaces;

// ReSharper disable UnusedType.Global
namespace ErtisAuth.Sdk.Services;

public class RoleService : MembershipBoundedService<Role>, IRoleService
{
	#region Properties
	
	protected override string Slug => "roles";	
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="ertisAuthOptions"></param>
	/// <param name="restHandler"></param>
	public RoleService(IErtisAuthOptions ertisAuthOptions, ISystemRestHandler restHandler) : base(ertisAuthOptions, restHandler)
	{
	
	}
	
	#endregion
	
	#region Methods
	
	public async Task<bool> CheckPermissionAsync(string rbac, TokenBase token, CancellationToken cancellationToken = default)
	{
		var url = $"{this.BaseUrl}/memberships/{this.MembershipId}/roles/check-permission";
		var queryString = QueryString.Add("permission", rbac);
		var headers = HeaderCollection.Add("Authorization", token.ToString());
		var response = await this.ExecuteRequestAsync(HttpMethod.Get, url, queryString, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
		return IsPermitted(response);
	}
	
	public async Task<bool> CheckPermissionByRoleAsync(string roleId, string rbac, TokenBase token, CancellationToken cancellationToken = default)
	{
		var url = $"{this.BaseUrl}/memberships/{this.MembershipId}/roles/{roleId}/check-permission";
		var queryString = QueryString.Add("permission", rbac);
		var headers = HeaderCollection.Add("Authorization", token.ToString());
		var response = await this.ExecuteRequestAsync(HttpMethod.Get, url, queryString, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
		return IsPermitted(response);
	}
	
	/// <summary>
	/// ErtisAuth answered whether the permission is granted; if it could not answer (unreachable, 5xx), the permission
	/// is unknown: AuthenticationServiceUnavailable instead of a denial.
	/// </summary>
	private static bool IsPermitted(IResponseResult response)
	{
		if (response.IsServiceUnavailable())
		{
			throw ErtisAuthException.AuthenticationServiceUnavailable();
		}
		
		return response.IsSuccess;
	}
	
	#endregion
}