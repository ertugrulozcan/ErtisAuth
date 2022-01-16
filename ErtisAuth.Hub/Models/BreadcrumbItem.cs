namespace ErtisAuth.Hub.Models
{
    public class BreadcrumbItem
    {
        #region Properties

        public string Title { get; init; }
        
        public string Controller { get; init; }
        
        public string Action { get; init; }
        
        public string RawUrl { get; init; }
        
        public object RouteParams { get; init; }

        #endregion
    }
}