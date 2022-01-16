namespace ErtisAuth.Hub.Configuration
{
    public interface IGoogleMapsApiConfiguration
    {
        string ApiKey { get; set; }
    }
	
    public class GoogleMapsApiConfiguration : IGoogleMapsApiConfiguration
    {
        public string ApiKey { get; set; }
    }
}