using Crm.Application.DTOs.Auth;
using FluentValidation;

namespace CrmApi.Validators.AuthValidator
{
    public class RegisterRequestValidator: AbstractValidator<AuthRegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ad alanı zorunludur")
                .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$").WithMessage("Ad sadece harf içerebilir");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Soyad alanı zorunludur")
                .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$").WithMessage("Soyad sadece harf içerebilir");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı zorunludur")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz")
                .MaximumLength(100).WithMessage("Email en fazla 100 karakter olabilir");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre alanı zorunludur")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır")
                .MaximumLength(50).WithMessage("Şifre en fazla 50 karakter olabilir")
                .Matches(@"[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir")
                .Matches(@"[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir")
                .Matches(@"[0-9]").WithMessage("Şifre en az bir rakam içermelidir");
        }
    }
}
