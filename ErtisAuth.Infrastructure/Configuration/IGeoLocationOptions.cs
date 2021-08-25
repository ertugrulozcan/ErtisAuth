namespace ErtisAuth.Infrastructure.Configuration
{
	public interface IGeoLocationOptions
	{
		#region Properties

		bool Enabled { get; set; }

		#endregion
	}
	
	public class GeoLocationOptions : IGeoLocationOptions
	{
		#region Properties

		public bool Enabled { get; set; }

		#endregion
	}
}