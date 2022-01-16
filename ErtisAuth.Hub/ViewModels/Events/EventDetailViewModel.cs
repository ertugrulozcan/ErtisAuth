using System;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Hub.ViewModels.Events
{
    public class EventDetailViewModel : ViewModelBase
    {
        #region Properties

        public string Id { get; set; }

        public string Type { get; set; }
		
        public string UtilizerId { get; set; }
		
        public DateTime EventTime { get; set; }
		
        public object Document { get; set; }
		
        public object Prior { get; set; }
		
        public User User { get; set; }
		
        public Application Application { get; set; }

        #endregion
    }
}