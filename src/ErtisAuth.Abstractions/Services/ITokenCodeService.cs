using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Abstractions.Services;

public interface ITokenCodeService : IMembershipBoundedService<TokenCode>
{
	/// <summary>
	/// Starts a device login: generates a user code by the membership's code policy and a device code.
	/// The plain device code is returned only here.
	/// </summary>
	Task<TokenCodeWithDeviceCode> CreateAsync(string membershipId, ClientInfo? clientInfo = null, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// The not expired code with the given user code, for the approval screen (the device that requested it and its status).
	/// </summary>
	Task<TokenCode> GetByUserCodeAsync(string userCode, string membershipId, CancellationToken cancellationToken = default);
	
	Task<TokenCode> ApproveAsync(string userCode, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
	
	Task<TokenCode> DenyAsync(string userCode, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Exchanges the device code of an approved code for a token of the approving user, once.
	/// </summary>
	Task<BearerToken> GenerateTokenAsync(string deviceCode, string membershipId, CancellationToken cancellationToken = default);
}
