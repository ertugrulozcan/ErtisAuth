namespace ErtisAuth.Infrastructure.Configuration
{
	public interface IMaxMindOptions
	{
		#region Properties
		
		string LicenseKey { get; set; }
		
		int AccountId { get; set; }
		
		#endregion
	}
	
	public class MaxMindOptions : IMaxMindOptions
	{
		#region Properties

		public string LicenseKey { get; set; }
		
		public int AccountId { get; set; }

		#endregion
	}
}