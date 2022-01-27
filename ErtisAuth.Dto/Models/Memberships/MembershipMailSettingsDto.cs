using ErtisAuth.Dto.Models.Mailing;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Memberships
{
    public class MembershipMailSettingsDto
    {
        #region Properties

        [BsonElement("smtp_server")]
        public SmtpServerDto SmtpServer { get; set; }

        #endregion
    }
}