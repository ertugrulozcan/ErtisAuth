using System;

namespace ErtisAuth.Hub.ViewModels
{
    [Serializable]
    public class SerializableViewModel : ViewModelBase
    {
        #region Constructors

        /// <summary>
        /// Parameterless Constructor
        /// </summary>
        public SerializableViewModel()
        {
			
        }
		
        /// <summary>
        /// Constructor 2
        /// </summary>
        /// <param name="parent"></param>
        public SerializableViewModel(ViewModelBase parent)
        {
            this.IsSuccess = parent.IsSuccess;
            this.ErrorMessage = parent.ErrorMessage;
            this.SuccessMessage = parent.SuccessMessage;
            this.Error = parent.Error;
            this.Errors = parent.Errors;
        }

        #endregion
    }
}