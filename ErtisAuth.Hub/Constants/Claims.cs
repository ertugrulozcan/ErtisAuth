namespace ErtisAuth.Hub.Constants
{
    public static class Claims
    {
        public const string UserId = "id";
        public const string Username = "username";
        public const string Email = "email";
        public const string FirstName = "firstName";
        public const string LastName = "lastName";
        public const string PhotoUrl = "photoUrl";
        public const string Role = "role";
        public const string MembershipId = "membershipId";
        public const string AccessToken = "access_token";
        public const string RefreshToken = "refresh_token";

        public static readonly string[] All = 
        {
            UserId,
            Username,
            Email,
            FirstName,
            LastName,
            PhotoUrl,
            Role,
            AccessToken,
            RefreshToken
        };
    }
}