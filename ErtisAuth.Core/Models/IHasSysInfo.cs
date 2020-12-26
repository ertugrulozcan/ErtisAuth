using Ertis.Core.Models.Resources;

namespace ErtisAuth.Core.Models
{
	public interface IHasSysInfo
	{
		#region Properties

		SysModel Sys { get; set; }

		#endregion
	}
}