using System;
using System.Linq;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Extensions.Mailkit.Models;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Mailing
{
    public class MailHook : MembershipBoundedResource, IHasSysInfo
    {
        #region Fields

        private string slug;

        #endregion
        
        #region Properties

        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("slug")]
        public string Slug
        {
            get
            {
                if (string.IsNullOrEmpty(this.slug))
                {
                    this.slug = Slugifier.Slugify(this.Name, Slugifier.Options.Ignore('_'));
                }

                return this.slug;
            }
            set => this.slug = Slugifier.Slugify(value, Slugifier.Options.Ignore('_'));
        }
		
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

        [JsonProperty("mailSubject")]
        public string MailSubject { get; set; }
        
        [JsonProperty("mailTemplate")]
        public string MailTemplate { get; set; }
        
        [JsonProperty("fromName")]
        public string FromName { get; set; }
        
        [JsonProperty("fromAddress")]
        public string FromAddress { get; set; }
        
        [JsonProperty("sendToUtilizer")]
        public bool SendToUtilizer { get; set; }
        
        [JsonProperty("recipients")]
        public Recipient[] Recipients { get; set; }
        
        [JsonProperty("mailProvider")]
        public string MailProvider { get; set; }
        
        [JsonProperty("sys")]
        public SysModel Sys { get; set; }
        
        #endregion
    }
}