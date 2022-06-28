using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Mailing
{
    public class SmtpServerDto
    {
        #region Properties

        [BsonElement("host")]
        public string Host { get; set; }
        
        [BsonElement("port")]
        public int Port { get; set; }
        
        [BsonElement("tls_enabled")]
        public bool TlsEnabled { get; set; }
        
        [BsonElement("username")]
        public string Username { get; set; }
        
        [BsonElement("password")]
        public string Password { get; set; }

        #endregion
    }
}