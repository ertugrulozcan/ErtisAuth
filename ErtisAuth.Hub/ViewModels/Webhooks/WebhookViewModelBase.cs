using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using ErtisAuth.Core.Models.Events;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ErtisAuth.Hub.ViewModels.Webhooks
{
    public class WebhookViewModelBase : ViewModelBase
    {
        #region Fields

        private List<SelectListItem> eventTypeList;
        private List<SelectListItem> httpMethods;

        #endregion
        
        #region Properties

        [Required]
        public string Name { get; set; }
		
        public string Description { get; set; }
		
        [Required]
        public string EventType { get; set; }
        
        public IEnumerable<SelectListItem> EventTypeList => 
            this.eventTypeList ??= Enum.GetNames(typeof(ErtisAuthEventType)).Select(x => new SelectListItem(x, x)).ToList();

        public bool IsActive { get; set; }

        [Required]
        public string RequestUrl { get; set; }
        
        [Required]
        public string RequestMethod { get; set; }
        
        public string RequestHeadersJson { get; set; }
        
        public Dictionary<string, object> RequestHeaders { get; set; }

        public string SelectedBodyType { get; set; } =
            ErtisAuth.Core.Models.Webhooks.WebhookRequestBodyType.Json.ToString();
        
        public string RequestBody { get; set; }

        public IEnumerable<SelectListItem> HttpMethods =>
            this.httpMethods ??= new List<SelectListItem>
            {
                new(HttpMethod.Get.Method, HttpMethod.Get.Method),
                new(HttpMethod.Post.Method, HttpMethod.Post.Method),
                new(HttpMethod.Put.Method, HttpMethod.Put.Method),
                new(HttpMethod.Delete.Method, HttpMethod.Delete.Method),
                new(HttpMethod.Head.Method, HttpMethod.Head.Method),
                new(HttpMethod.Options.Method, HttpMethod.Options.Method),
                new(HttpMethod.Patch.Method, HttpMethod.Patch.Method),
                new(HttpMethod.Trace.Method, HttpMethod.Trace.Method)
            };

        [Required]
        [Range(1, 5)]
        public int TryCount { get; set; }
		
        #endregion

        #region Methods

        public bool TryGetHeaders(out Dictionary<string, object> headers, out Exception exception)
        {
            if (!string.IsNullOrEmpty(this.RequestHeadersJson))
            {
                try
                {
                    var initialRequestHeaders = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<KeyValuePair<string, string>>>(this.RequestHeadersJson);
                    headers = initialRequestHeaders?
                        .Where(x => !string.IsNullOrEmpty(x.Key))
                        .ToDictionary(x => x.Key, y => y.Value as object);

                    exception = null;
                    return true;
                }
                catch (Exception ex)
                {
                    headers = null;
                    exception = ex;
                    return false;
                }
            }
            
            headers = null;
            exception = null;
            return true;
        }

        public bool TryGetBody(out object body, out Exception exception)
        {
            if (!string.IsNullOrEmpty(this.RequestBody))
            {
                try
                {
                    body = Newtonsoft.Json.JsonConvert.DeserializeObject(this.RequestBody);
                    exception = null;
                    return true;
                }
                catch (Exception ex)
                {
                    body = null;
                    exception = ex;
                    return false;
                }
            }
            
            body = null;
            exception = null;
            return true;
        }

        #endregion
    }
}