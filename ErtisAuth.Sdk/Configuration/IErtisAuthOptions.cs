namespace ErtisAuth.Sdk.Configuration
{
	public interface IErtisAuthOptions
	{
		#region Properties

		string BaseUrl { get; }
		
		string MembershipId { get; }
		
		string ApplicationId { get; }
		
		string Secret { get; }
		
		string BasicToken { get; }

		#endregion
	}
	
	public class ErtisAuthOptions : IErtisAuthOptions
	{
		#region Properties

		public string BaseUrl { get; set; }
		
		public string MembershipId { get; set; }
		
		public string ApplicationId { get; set; }
		
		public string Secret { get; set; }

		public string BasicToken => $"{this.ApplicationId}:{this.Secret}";

		#endregion
	}
}