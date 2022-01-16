namespace ErtisAuth.Hub.Helpers
{
    public static class HtmlTag
    {
        #region Methods

        public static string Css(string path)
        {
            return "<link href=\"../" + path.TrimStart('/') + "?v=" + VersionManager.Version + "\" rel=\"stylesheet\" type=\"text/css\" />";
        }
		
        public static string Js(string path)
        {
            return "<script src=\"../" + path.TrimStart('/') + "?v=" + VersionManager.Version + "\" type=\"text/javascript\"></script>";
        }

        #endregion
    }
}