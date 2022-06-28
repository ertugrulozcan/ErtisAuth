using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
    public interface IDeletableResourceService
    {
        IResponseResult Delete(string modelId, TokenBase token);
		
        Task<IResponseResult> DeleteAsync(string modelId, TokenBase token);
        
        IResponseResult BulkDelete(IEnumerable<string> modelIds, TokenBase token);
		
        Task<IResponseResult> BulkDeleteAsync(IEnumerable<string> modelIds, TokenBase token);
    }
}