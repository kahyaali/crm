using Crm.Application.DTOs.Personel;
using Crm.Infrastructure.Data;
using Crm.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Validators.PersonelValidator
{
    public class UpdatePersonelDtoValidator : AbstractValidator<UpdatePersonelDto>
    {
       
        public UpdatePersonelDtoValidator()
        {
        

            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Geçersiz personel ID");

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

            RuleFor(x => x.HireDate)
                .NotEmpty().WithMessage("İşe başlama tarihi zorunludur")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("İşe başlama tarihi gelecek tarih olamaz")
                .GreaterThanOrEqualTo(new DateTime(2000, 1, 1)).WithMessage("İşe başlama tarihi çok eski");

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("Geçerli bir departman seçiniz")
                .When(x => x.DepartmentId.HasValue);

            RuleFor(x => x.PositionId)
                .GreaterThan(0).WithMessage("Geçerli bir pozisyon seçiniz")
                .When(x => x.PositionId.HasValue);

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
