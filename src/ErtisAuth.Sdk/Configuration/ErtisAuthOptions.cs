// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Sdk.Configuration;

public interface IErtisAuthOptions
{
	#region Properties
	
	string? BaseUrl { get; }
	
	string? MembershipId { get; }
	
	int? BasicTokenCacheTTL { get; }
	
	#endregion
}

public class ErtisAuthOptions : IErtisAuthOptions
{
	#region Properties
	
	public string? BaseUrl { get; set; }
	
	public string? MembershipId { get; set; }
	
	public int? BasicTokenCacheTTL { get; set; }
	
	#endregion
}