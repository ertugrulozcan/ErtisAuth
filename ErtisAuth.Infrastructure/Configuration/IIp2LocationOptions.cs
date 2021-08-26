namespace ErtisAuth.Infrastructure.Configuration
{
	public interface IIp2LocationOptions
	{
		#region Properties
		
		string LicenseKey { get; set; }
		
		string Package { get; set; }
		
		#endregion
	}
	
	public class Ip2LocationOptions : IIp2LocationOptions
	{
		#region Properties

		public string LicenseKey { get; set; }
		
		public string Package { get; set; }

		#endregion
	}
}