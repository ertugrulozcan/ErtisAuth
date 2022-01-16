using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ErtisAuth.Core.Models.Users;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ErtisAuth.Hub.ViewModels.Memberships
{
    public abstract class MembershipViewModelBase : ViewModelBase
    {
        #region Properties

        [Required]
        public string Name { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int ExpiresIn { get; set; }
		
        [Required]
        [Range(1, int.MaxValue)]
        public int RefreshTokenExpiresIn { get; set; }
		
        [Required]
        [StringLength(32, MinimumLength = 32, ErrorMessage = "Secret key must be 32 character")]
        public string SecretKey { get; set; }
		
        [Required]
        public string HashAlgorithm { get; set; }

        [Required]
        public string DefaultEncoding { get; set; }
        
        public List<SelectListItem> HashAlgorithmList { get; set; }
		
        public List<SelectListItem> EncodingList { get; set; }
		
        public UserType UserType { get; set; }
        
        #endregion
    }
}