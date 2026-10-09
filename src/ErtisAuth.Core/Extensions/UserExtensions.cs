using Ertis.Core.Collections;
using Ertis.Schema.Dynamics;

namespace ErtisAuth.Core.Extensions;

public static class UserExtensions
{
	#region Methods
	
	public static void HidePasswordHash(this DynamicObject? user)
	{
		user?.RemoveProperty("password_hash");
	}
	
	public static IPaginationCollection<DynamicObject> HidePasswordHash(this IPaginationCollection<DynamicObject> results)
	{
		var users = results.Items.ToArray();
		foreach (var user in users)
		{
			user.RemoveProperty("password_hash");
		}
		
		return new PaginationCollection<DynamicObject>
		{
			Count = results.Count,
			Items = users
		};
	}
	
	#endregion
}