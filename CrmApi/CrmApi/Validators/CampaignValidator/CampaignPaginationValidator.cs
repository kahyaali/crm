using Crm.Application.DTOs.Campaign;
using FluentValidation;

namespace CrmApi.Validators.CampaignValidator
{
    public class CampaignPaginationValidator : AbstractValidator<CampaignPaginationDto>
    {
        public CampaignPaginationValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası en az 1 olmalıdır");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1-100 arasında olmalıdır");

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Taslak", "Aktif", "Tamamlandı", "İptal" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");

            RuleFor(x => x.Type)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Email", "SMS", "Sosyal Medya", "Radyo", "TV" }.Contains(s))
                .WithMessage("Geçersiz kampanya tipi");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("Başlangıç tarihi, bitiş tarihinden büyük olamaz");
        }
    }
}
