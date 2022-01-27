using ErtisAuth.Core.Models.Mailing;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Memberships
{
    public class MembershipMailSettings
    {
        #region Properties

        [JsonProperty("smtp_server")]
        public SmtpServer SmtpServer { get; set; }

        #endregion
    }
}