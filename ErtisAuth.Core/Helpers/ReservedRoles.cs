namespace ErtisAuth.Core.Helpers
{
    public static class ReservedRoles
    {
        public const string Administrator = "admin";
        public const string Server = "server";

        public static string[] ToArray()
        {
            return new[]
            {
                Administrator,
                Server
            };
        }
    }
}