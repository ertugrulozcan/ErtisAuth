namespace ErtisAuth.Core.Models.GeoLocation
{
	public class GeoLocationInfo
	{
		#region Properties

		public string City { get; set; }
		
		public string Continent { get; set; }
		
		public string Country { get; set; }
		
		public string PostalCode { get; set; }
		
		public Coordinate Location { get; set; }

		#endregion
	}
}