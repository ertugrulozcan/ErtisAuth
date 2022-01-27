using System.Linq;

namespace ErtisAuth.Hub.Helpers
{
    public static class HtmlTag
    {
        #region Methods

        public static string Css(string path, int level = -1)
        {
            var directoryLevel = level < 0 ? "/" : (level == 1 ? "../" : string.Join(string.Empty, Enumerable.Range(0, level).Select(x => "../")));  
            return "<link href=\"" + directoryLevel + path.TrimStart('/') + "?v=" + VersionManager.Version + "\" rel=\"stylesheet\" type=\"text/css\" />";
        }
		
        public static string Js(string path, int level = -1)
        {
            var directoryLevel = level < 0 ? "/" : (level == 1 ? "../" : string.Join(string.Empty, Enumerable.Range(0, level).Select(x => "../")));
            return "<script src=\"" + directoryLevel + path.TrimStart('/') + "?v=" + VersionManager.Version + "\" type=\"text/javascript\"></script>";
        }

        #endregion
    }
}