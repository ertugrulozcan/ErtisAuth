using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Hub.ViewModels.Users
{
    public interface IHasUserTypeUserViewModel
    {
        UserType UserType { get; set; }
		
        dynamic AdditionalProperties { get; set; }
    }
}