using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ErtisAuth.Core.Models.Events;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ErtisAuth.Hub.ViewModels.Memberships
{
    public class MailHookViewModelBase : ViewModelBase
    {
        #region Fields

        private List<SelectListItem> eventTypeList;

        #endregion
        
        #region Properties

        [Required]
        public string Name { get; set; }
		
        public string Description { get; set; }
		
        [Required]
        public string EventType { get; set; }
        
        public IEnumerable<SelectListItem> EventTypeList => 
            this.eventTypeList ??= Enum.GetNames(typeof(ErtisAuthEventType)).Select(x => new SelectListItem(x, x)).ToList();

        public bool IsActive { get; set; }
        
        [Required]
        public string MailSubject { get; set; }
        
        [Required]
        public string MailTemplate { get; set; }

        [Required]
        public string FromName { get; set; }
        
        [Required]
        public string FromAddress { get; set; }
        
        [Required]
        public string MembershipId { get; set; }

        #endregion
    }
}