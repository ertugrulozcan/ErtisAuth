using ErtisAuth.Core.Models.GeoLocation;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dto.Models.GeoLocation;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class ClientInfoExtensions
{
    #region Methods

    public static ClientInfo ToModel(this ClientInfoDto dto)
    {
        return new ClientInfo
        {
            IPAddress = dto.IPAddress,
            UserAgent = dto.UserAgent,
            GeoLocation = dto.GeoLocation != null ? new GeoLocationInfo
            {
                City = dto.GeoLocation.City,
                Country = dto.GeoLocation.Country,
                CountryCode = dto.GeoLocation.CountryCode,
                PostalCode = dto.GeoLocation.PostalCode,
                Location = dto.GeoLocation.Location != null ? new Coordinate
                {
                    Latitude = dto.GeoLocation.Location.Latitude,
                    Longitude = dto.GeoLocation.Location.Longitude
                } : null,
                Isp = dto.GeoLocation.Isp,
                IspDomain = dto.GeoLocation.IspDomain
            } : null
        };
    }
	
    public static ClientInfoDto ToDto(this ClientInfo clientInfo)
    {
        var geoLocation = clientInfo.GeoLocation;
        GeoLocationInfoDto geoLocationDto = null;
        if (geoLocation != null)
        {
            geoLocationDto = new GeoLocationInfoDto
            {
                City = geoLocation.City,
                Country = geoLocation.Country,
                CountryCode = geoLocation.CountryCode,
                PostalCode = geoLocation.PostalCode,
                Location = geoLocation.Location != null ? new CoordinateDto
                {
                    Latitude = geoLocation.Location.Latitude,
                    Longitude = geoLocation.Location.Longitude
                } : null,
                Isp = geoLocation.Isp,
                IspDomain = geoLocation.IspDomain
            };
        }
		
        return new ClientInfoDto
        {
            IPAddress = clientInfo.IPAddress,
            UserAgent = clientInfo.UserAgent,
            GeoLocation = geoLocationDto
        };
    }

    #endregion
}