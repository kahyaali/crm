namespace Crm.Application.DTOs.Auth
{
    public class AuthRefreshRequest
    {
        public string RefreshToken { get; set; }
        public string AccessToken { get; set; }
    }
}
