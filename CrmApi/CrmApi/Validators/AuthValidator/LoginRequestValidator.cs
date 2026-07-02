using Crm.Application.DTOs.Auth;
using FluentValidation;

namespace CrmApi.Validators.AuthValidator
{
    public class LoginRequestValidator: AbstractValidator<AuthLoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı zorunludur")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz")
                .MaximumLength(100).WithMessage("Email en fazla 100 karakter olabilir");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre alanı zorunludur")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır")
                .MaximumLength(50).WithMessage("Şifre en fazla 50 karakter olabilir");
        }
    }
}
