using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Sdk.Services.Interfaces;

public interface IDeletableResourceService
{
	IResponseResult Delete(string modelId, TokenBase token);
	
	Task<IResponseResult> DeleteAsync(string modelId, TokenBase token, CancellationToken cancellationToken = default);
	
	IResponseResult BulkDelete(IEnumerable<string> modelIds, TokenBase token);
	
	Task<IResponseResult> BulkDeleteAsync(IEnumerable<string> modelIds, TokenBase token, CancellationToken cancellationToken = default);
}