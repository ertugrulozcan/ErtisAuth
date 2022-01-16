namespace ErtisAuth.Hub.Extensions
{
    public static class StringExtensions
    {
        #region Methods

        public static string Capitalize(this string str)
        {
            return str.Length switch
            {
                0 => str,
                1 => str.ToUpper(),
                _ => char.ToUpper(str[0]) + str.Substring(1)
            };
        }

        #endregion
    }
}