using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Mailing
{
    public class SmtpServer
    {
        #region Properties

        [JsonProperty("host")]
        public string Host { get; set; }
        
        [JsonProperty("port")]
        public int Port { get; set; }
        
        [JsonProperty("tls_enabled")]
        public bool TlsEnabled { get; set; }
        
        [JsonProperty("username")]
        public string Username { get; set; }
        
        [JsonProperty("password")]
        public string Password { get; set; }

        #endregion
    }
}