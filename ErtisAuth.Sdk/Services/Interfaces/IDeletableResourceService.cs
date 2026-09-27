using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Sdk.Services.Interfaces;

public interface IDeletableResourceService
{
	Task<IResponseResult> DeleteAsync(string modelId, TokenBase token, CancellationToken cancellationToken = default);
	
	Task<IResponseResult> BulkDeleteAsync(IEnumerable<string> modelIds, TokenBase token, CancellationToken cancellationToken = default);
}