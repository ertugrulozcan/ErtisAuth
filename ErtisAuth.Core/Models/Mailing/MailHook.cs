using System;
using System.Linq;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Events;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Mailing
{
    public class MailHook : MembershipBoundedResource, IHasSysInfo
    {
        #region Properties

        [JsonProperty("name")]
        public string Name { get; set; }
		
        [JsonProperty("description")]
        public string Description { get; set; }
		
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonIgnore]
        public ErtisAuthEventType? EventType
        {
            get
            {
                if (Enum.GetNames(typeof(ErtisAuthEventType)).Any(x => x == this.Event))
                {
                    return (ErtisAuthEventType) Enum.Parse(typeof(ErtisAuthEventType), this.Event);
                }
                else
                {
                    return null;
                }
            }
        }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonIgnore]
        public bool IsActive => this.Status == "active";

        [JsonProperty("mail_subject")]
        public string MailSubject { get; set; }
        
        [JsonProperty("mail_template")]
        public string MailTemplate { get; set; }
        
        [JsonProperty("from_name")]
        public string FromName { get; set; }
        
        [JsonProperty("from_address")]
        public string FromAddress { get; set; }
        
        [JsonProperty("sys")]
        public SysModel Sys { get; set; }
        
        #endregion
    }
}