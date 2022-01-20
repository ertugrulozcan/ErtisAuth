namespace ErtisAuth.Hub.ViewModels.Auth
{
    public class ForgotPasswordRequest
    {
        #region Properties

        public string ServerUrl { get; set; }
        
        public string Host { get; set; }
        
        public string EmailAddress { get; set; }
        
        public string EncryptedSecretKey { get; set; }
        
        public string MembershipId { get; set; }

        public string ClearHost
        {
            get
            {
                if (string.IsNullOrEmpty(this.Host))
                {
                    return this.Host;
                }
                
                var host = this.Host;
                host = host.Replace("https://", string.Empty);
                host = host.Replace("http://", string.Empty);
                host = host.TrimEnd('/');

                return host;
            }
        }
        
        #endregion
    }
}