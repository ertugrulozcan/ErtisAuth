using System.Collections.Generic;
using System.Linq;
using ErtisAuth.Hub.Models;

namespace ErtisAuth.Hub.Helpers
{
    public static class BreadcrumbBuilder
    {
        #region Methods

        public static IEnumerable<BreadcrumbItem> Add(
            string title, 
            string action, 
            string controller,
            object routeParams = null,
            string rawUrl = null)
        {
            var breadcrumbItem = new BreadcrumbItem
            {
                Title = title,
                Action = action,
                Controller = controller,
                RouteParams = routeParams,
                RawUrl = rawUrl
            };

            return new[]
            {
                breadcrumbItem
            };
        }
        
        public static IEnumerable<BreadcrumbItem> Add(
            this IEnumerable<BreadcrumbItem> breadcrumb,
            string title, 
            string action, 
            string controller,
            object routeParams = null,
            string rawUrl = null)
        {
            var breadcrumbItem = new BreadcrumbItem
            {
                Title = title,
                Action = action,
                Controller = controller,
                RouteParams = routeParams,
                RawUrl = rawUrl
            };

            return breadcrumb.Append(breadcrumbItem);
        }

        #endregion
    }
}