using System;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Net.Rest;
using Ertis.Net.Services;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.GeoLocation;
using ErtisAuth.Infrastructure.Configuration;
using ErtisAuth.Infrastructure.Models.GeoLocation;

namespace ErtisAuth.Infrastructure.Services
{
	public class GeoLocationService : RestService, IGeoLocationService
	{
		#region Services

		private readonly IIp2LocationOptions ip2LocationOptions;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="restHandler"></param>
		/// <param name="ip2LocationOptions"></param>
		public GeoLocationService(IRestHandler restHandler, IIp2LocationOptions ip2LocationOptions) : base(restHandler)
		{
			this.ip2LocationOptions = ip2LocationOptions;
		}

		#endregion
		
		#region Methods

		public async Task<GeoLocationInfo> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
		{
			var response = await this.GetAsync<Ip2LocationLookupResponse>($"https://api.ip2location.com/v2/?ip={ipAddress}&key={this.ip2LocationOptions.LicenseKey}&package={this.ip2LocationOptions.Package}", cancellationToken: cancellationToken);
			if (response.IsSuccess)
			{
				double? nullableLatitude = null;
				if (double.TryParse(response.Data.Latitude, out var latitude))
				{
					nullableLatitude = latitude;
				}
				
				double? nullableLongitude = null;
				if (double.TryParse(response.Data.Longitude, out var longitude))
				{
					nullableLongitude = longitude;
				}

				return new GeoLocationInfo
				{
					City = response.Data.CityName,
					Country = response.Data.CountryName,
					CountryCode = response.Data.CountryCode,
					PostalCode = response.Data.ZipCode,
					Isp = response.Data.Isp,
					IspDomain = response.Data.Domain,
					Location = new Coordinate
					{
						Latitude = nullableLatitude,
						Longitude = nullableLongitude
					}
				};
			}
			else
			{
				Console.WriteLine($"GeoLocationService.LookupAsync({ipAddress}) error : {response.Message}");
				return null;
			}
		}
		
		#endregion
	}

	public class GeoLocationDisabledService : IGeoLocationService
	{
		#region Methods

		public Task<GeoLocationInfo> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
		{
			return null;
		}

		#endregion
	}
}