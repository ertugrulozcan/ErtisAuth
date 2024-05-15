using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.GeoLocation;
using ErtisAuth.Infrastructure.Configuration;

namespace ErtisAuth.Infrastructure.Services
{
	public class MaxMindGeoLocationService : IGeoLocationService
	{
		#region Services

		private readonly IMaxMindOptions maxMindOptions;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="maxMindOptions"></param>
		public MaxMindGeoLocationService(IMaxMindOptions maxMindOptions)
		{
			this.maxMindOptions = maxMindOptions;
		}

		#endregion
		
		#region Methods
		
		public async Task<GeoLocationInfo> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
		{
			using var client = new MaxMind.GeoIP2.WebServiceClient(this.maxMindOptions.AccountId, this.maxMindOptions.LicenseKey, host: "geolite.info", timeout: 10000);
			var response = await client.CityAsync(ipAddress);
			return new GeoLocationInfo
			{
				City = response.City.Names["en"],
				Country = response.Country.Names["en"],
				CountryCode = response.Country.IsoCode,
				PostalCode = response.Postal.Code,
				Isp = response.Traits.Isp,
				IspDomain = response.Traits.AutonomousSystemOrganization,
				Location = new Coordinate
				{
					Latitude = response.Location.Latitude,
					Longitude = response.Location.Longitude
				}
			};
		}

		#endregion
	}
}