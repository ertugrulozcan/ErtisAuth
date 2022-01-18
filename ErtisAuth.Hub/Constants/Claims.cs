namespace ErtisAuth.Hub.Constants
{
    public static class Claims
    {
        public const string ServerUrl = "server_url";
        public const string UserId = "id";
        public const string Username = "username";
        public const string Email = "email";
        public const string FirstName = "firstName";
        public const string LastName = "lastName";
        public const string Role = "role";
        public const string MembershipId = "membership_id";
		public const string MembershipName = "membership_name";
        public const string AccessToken = "access_token";
        public const string RefreshToken = "refresh_token";

        public static readonly string[] All = 
        {
            ServerUrl,
            MembershipId,
			MembershipName,
            UserId,
            Username,
            Email,
            FirstName,
            LastName,
            Role,
            AccessToken,
            RefreshToken
        };
    }
}