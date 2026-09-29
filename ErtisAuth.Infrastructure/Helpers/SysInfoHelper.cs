using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Infrastructure.Helpers;

/// <summary>
/// The one rule for the sys info (who created / last modified a resource, and when) of every resource.
/// Resources implementing <see cref="IHasSysInfo"/> get it automatically through the create and update methods of the
/// CRUD base services (MembershipBoundedCrudService, GenericCrudService); only writes bypassing them use it directly.
/// Times are UTC; the utilizer is written by its username (user), slug (application) or "system".
/// </summary>
public static class SysInfoHelper
{
	#region Methods
	
	public static SysModel Created(Utilizer utilizer)
	{
		return new SysModel
		{
			CreatedAt = DateTime.UtcNow,
			CreatedBy = GetUtilizerName(utilizer)
		};
	}
	
	/// <summary>
	/// The sys info of an update: the creation info of the current version is kept, the modification info is set.
	/// A current version without creation info (should not happen) gets the update as its creation, as on master.
	/// </summary>
	public static SysModel Modified(SysModel? current, Utilizer utilizer)
	{
		var now = DateTime.UtcNow;
		var utilizerName = GetUtilizerName(utilizer);
		return new SysModel
		{
			CreatedAt = current?.CreatedAt ?? now,
			CreatedBy = current?.CreatedBy ?? utilizerName,
			ModifiedAt = now,
			ModifiedBy = utilizerName
		};
	}
	
	/// <summary>
	/// Sets the creation info on a resource to be inserted; a sys info sent by the caller is ignored.
	/// </summary>
	public static void SetCreated(object model, Utilizer utilizer)
	{
		if (model is IHasSysInfo resource)
		{
			resource.Sys = Created(utilizer);
		}
	}
	
	/// <summary>
	/// Sets the modification info on a resource to be updated, from the stored version (a sys info sent by the caller is ignored).
	/// </summary>
	public static void SetModified(object model, object current, Utilizer utilizer)
	{
		if (model is IHasSysInfo resource)
		{
			resource.Sys = Modified((current as IHasSysInfo)?.Sys, utilizer);
		}
	}
	
	private static string GetUtilizerName(Utilizer utilizer)
	{
		var _utilizer = utilizer.Type == Utilizer.UtilizerType.System ? Utilizer.GetSystemUtilizer(string.Empty) : utilizer;
		return _utilizer.Username;
	}
	
	#endregion
}
