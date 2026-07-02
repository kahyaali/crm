namespace Crm.Application.DTOs.Auth
{
    public class AuthRegisterRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
