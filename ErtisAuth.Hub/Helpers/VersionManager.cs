namespace ErtisAuth.Hub.Helpers
{
    public static class VersionManager
    {
        private static string version;
        public static string Version
        {
            get
            {
                if (string.IsNullOrEmpty(version))
                {
                    version = $"{Program.GetEnvironmentParameter("Version")}";
                }

                return version;
            }
        }
    }
}