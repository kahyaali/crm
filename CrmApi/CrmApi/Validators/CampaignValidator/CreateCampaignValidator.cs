using Crm.Application.DTOs.Campaign;
using FluentValidation;

namespace CrmApi.Validators.CampaignValidator
{
    public class CreateCampaignValidator : AbstractValidator<CreateCampaignDto>
    {
        public CreateCampaignValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Kampanya adı zorunludur")
                .MaximumLength(200).WithMessage("Kampanya adı en fazla 200 karakter olabilir");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Başlangıç tarihi zorunludur");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("Bitiş tarihi zorunludur")
                .GreaterThan(x => x.StartDate)
                .WithMessage("Bitiş tarihi, başlangıç tarihinden sonra olmalıdır");

            RuleFor(x => x.Budget)
                .GreaterThanOrEqualTo(0).WithMessage("Bütçe 0 veya daha büyük olmalıdır");

            RuleFor(x => x.Type)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Email", "SMS", "Sosyal Medya", "Radyo", "TV" }.Contains(s))
                .WithMessage("Geçersiz kampanya tipi");

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Taslak", "Aktif", "Tamamlandı", "İptal" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");
        }
    }
}
