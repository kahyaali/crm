using Crm.Application.DTOs.Personel;
using Crm.Infrastructure.Data;
using Crm.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Validators.PersonelValidator
{
    public class CreatePersonelDtoValidator : AbstractValidator<CreatePersonelDto>
    {
        
        public CreatePersonelDtoValidator()
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

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Telefon alanı zorunludur")
                .MaximumLength(20).WithMessage("Telefon en fazla 20 karakter olabilir")
                .Matches(@"^[0-9+\s-]+$").WithMessage("Geçerli bir telefon numarası giriniz");
        
               

            RuleFor(x => x.Salary)
                .GreaterThanOrEqualTo(0).WithMessage("Maaş 0'dan küçük olamaz")
                .LessThanOrEqualTo(1000000).WithMessage("Maaş çok yüksek");

            // HireDate - sadece varsa kontrol et
            RuleFor(x => x.HireDate)
                .Must(x => x <= DateTime.Now).WithMessage("İşe başlama tarihi gelecek tarih olamaz")
                .When(x => x.HireDate.HasValue);

            // Password - sadece CreateUser true ise ve doluysa kontrol et
            RuleFor(x => x.Password)
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır")
                .When(x => x.CreateUser && !string.IsNullOrEmpty(x.Password));
            // Personel No
            RuleFor(x => x.PersonnelNumber)
                .MaximumLength(10).WithMessage("Personel No en fazla 10 karakter olabilir")
                .Matches(@"^[a-zA-Z0-9\-_]+$").WithMessage("Personel No harf, rakam, - ve _ içerebilir")
                .When(x => !string.IsNullOrEmpty(x.PersonnelNumber));

            // Sicil No
            RuleFor(x => x.RegistrationNumber)
                .MaximumLength(10).WithMessage("Sicil No en fazla 10 karakter olabilir")
                .Matches(@"^[a-zA-Z0-9\-_]+$").WithMessage("Sicil No harf, rakam, - ve _ içerebilir")
                .When(x => !string.IsNullOrEmpty(x.RegistrationNumber));

        }
    }
}
