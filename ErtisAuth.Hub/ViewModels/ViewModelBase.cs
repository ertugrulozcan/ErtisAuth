using System.Collections.Generic;
using Ertis.Core.Models.Response;

namespace ErtisAuth.Hub.ViewModels
{
    public abstract class ViewModelBase
    {
        #region Fields

        private ErrorModel errorModel;

        #endregion
		
        #region Properties

        public bool? IsSuccess { get; set; }
		
        public string SuccessMessage { get; set; }
		
        public string ErrorMessage { get; set; }
		
        public IEnumerable<string> Errors { get; set; }
		
        public ErrorModel Error 
        {
            get => this.errorModel;
            set
            {
                this.errorModel = value;
                if (this.errorModel != null)
                {
                    this.IsSuccess = false;
                    this.ErrorMessage = this.errorModel.Message;
                }
            }
        }

        #endregion
    }
}