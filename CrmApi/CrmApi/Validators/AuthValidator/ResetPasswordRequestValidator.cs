using Crm.Application.DTOs.Auth;
using FluentValidation;

namespace CrmApi.Validators.AuthValidator
{
    public class ResetPasswordRequestValidator : AbstractValidator<AuthResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı zorunludur")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token alanı zorunludur");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Yeni şifre alanı zorunludur")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır")
                .MaximumLength(50).WithMessage("Şifre en fazla 50 karakter olabilir")
                .Matches(@"[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir")
                .Matches(@"[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir")
                .Matches(@"[0-9]").WithMessage("Şifre en az bir rakam içermelidir");
        }
    }
}
