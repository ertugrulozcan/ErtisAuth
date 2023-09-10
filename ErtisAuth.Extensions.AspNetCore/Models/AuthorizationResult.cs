using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Extensions.AspNetCore.Models;

public class AuthorizationResult
{
	#region Properties

	public Utilizer Utilizer { get; }
	
	public bool IsAuthorized { get; }
	
	public Rbac Rbac { get; }

	#endregion

	#region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="utilizer"></param>
	/// <param name="rbac"></param>
	/// <param name="isAuthorized"></param>
	public AuthorizationResult(Utilizer utilizer, Rbac rbac, bool isAuthorized)
	{
		this.Utilizer = utilizer;
		this.Rbac = rbac;
		this.IsAuthorized = isAuthorized;
	}

	#endregion
}