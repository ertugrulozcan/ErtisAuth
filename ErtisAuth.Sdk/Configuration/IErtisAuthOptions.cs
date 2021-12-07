namespace ErtisAuth.Sdk.Configuration
{
	public interface IErtisAuthOptions
	{
		#region Properties

		string BaseUrl { get; }
		
		string MembershipId { get; }

		#endregion
	}
	
	public class ErtisAuthOptions : IErtisAuthOptions
	{
		#region Properties

		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		public string BaseUrl { get; set; }
		
		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		public string MembershipId { get; set; }

		#endregion
	}
}