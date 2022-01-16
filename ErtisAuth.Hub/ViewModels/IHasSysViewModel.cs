using Ertis.Core.Models.Resources;

namespace ErtisAuth.Hub.ViewModels
{
    public interface IHasSysViewModel
    {
        SysModel Sys { get; set; }
    }
}