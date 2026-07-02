using Crm.Application.DTOs.Lead;
using FluentValidation;

namespace CrmApi.Validators.LeadValidator
{
    public class UpdateLeadValidator : AbstractValidator<UpdateLeadDto>
    {
        public UpdateLeadValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Geçersiz lead ID");

            RuleFor(x => x.CompanyName)
                .NotEmpty().WithMessage("Firma adı zorunludur")
                .MaximumLength(200).WithMessage("Firma adı en fazla 200 karakter olabilir");

            RuleFor(x => x.ContactName)
                .NotEmpty().WithMessage("Yetkili kişi zorunludur")
                .MaximumLength(100).WithMessage("Yetkili kişi en fazla 100 karakter olabilir");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email zorunludur")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz")
                .MaximumLength(100).WithMessage("Email en fazla 100 karakter olabilir");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Telefon zorunludur")
                .MaximumLength(20).WithMessage("Telefon en fazla 20 karakter olabilir")
                .Matches(@"^[0-9+\s\(\)-]+$").WithMessage("Geçerli bir telefon numarası giriniz");

            RuleFor(x => x.Source)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Web", "Referans", "Reklam", "Fuar", "SosyalMedya", "Email", "Telefon" }.Contains(s))
                .WithMessage("Geçersiz kaynak değeri");

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Yeni", "IletisimeGecildi", "TeklifSunuldu", "MusteriOldu", "Kaybedildi" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");

            RuleFor(x => x.PotentialRevenue)
                .GreaterThanOrEqualTo(0).When(x => x.PotentialRevenue.HasValue)
                .WithMessage("Potansiyel gelir 0'dan küçük olamaz");

            RuleFor(x => x.NextFollowUpDate)
                .GreaterThanOrEqualTo(DateTime.Today).When(x => x.NextFollowUpDate.HasValue)
                .WithMessage("Takip tarihi bugünden önce olamaz");
        }
    }
}
