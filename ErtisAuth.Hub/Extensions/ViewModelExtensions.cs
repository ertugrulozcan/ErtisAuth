using System.Linq;
using ErtisAuth.Hub.Helpers;
using ErtisAuth.Hub.ViewModels;
using Ertis.Core.Models.Response;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ErtisAuth.Hub.Extensions
{
    public static class ViewModelExtensions
    {
        #region Methods

        public static void SetError(this ViewModelBase model, IResponseResult responseResult)
        {
            if (model == null || responseResult == null)
            {
                return;
            }

            if (!responseResult.IsSuccess)
            {
                model.IsSuccess = false;
                if (ErrorHelper.TryGetErrors(responseResult, out var errors) && errors?.Data != null && errors.Data.Any(x => !string.IsNullOrEmpty(x)))
                {
                    model.Errors = errors.Data;
                }
                else if (ErrorHelper.TryGetError(responseResult, out var error))
                {
                    model.Error = error;
                }
                else
                {
                    model.ErrorMessage = responseResult.Message;
                }	
            }
        }
        
        public static void SetError(this ViewModelBase model, ModelStateDictionary modelState, string title = null)
        {
            if (model == null || modelState == null)
            {
                return;
            }

            model.IsSuccess = false;
            model.ErrorMessage = title;
            model.Errors = modelState.Values.SelectMany(x => x.Errors.Select(y => y.ErrorMessage));
        }

        #endregion
    }
}