using System.Collections.Generic;
using Ertis.Core.Models.Response;
using Newtonsoft.Json;

namespace ErtisAuth.Hub.Helpers
{
    public static class ErrorHelper
    {
        #region Methods

        public static bool TryGetErrors(IResponseResult responseResult, out ErrorModel<IEnumerable<string>> error)
        {
            try
            {
                error = JsonConvert.DeserializeObject<ErrorModel<IEnumerable<string>>>(responseResult.Json);
                return true;
            }
            catch
            {
                error = null;
                return false;
            }
        }
		
        public static bool TryGetError(IResponseResult responseResult, out ErrorModel error)
        {
            try
            {
                error = JsonConvert.DeserializeObject<ErrorModel>(responseResult.Json);
                return true;
            }
            catch
            {
                error = null;
                return false;
            }
        }

        #endregion
    }
}