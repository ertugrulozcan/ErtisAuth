using System;

namespace ErtisAuth.Hub.ViewModels
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
		
        public Exception Exception { get; set; }
    }
}