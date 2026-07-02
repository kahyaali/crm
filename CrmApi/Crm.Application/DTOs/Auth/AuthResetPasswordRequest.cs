namespace Crm.Application.DTOs.Auth
{
    public class AuthResetPasswordRequest
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
