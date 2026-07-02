using Crm.Application.DTOs.Opportunity;
using FluentValidation;

namespace CrmApi.Validators.OpportunityValidator
{
    public class OpportunityPaginationValidator : AbstractValidator<OpportunityPaginationDto>
    {
        public OpportunityPaginationValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası en az 1 olmalıdır");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1-100 arasında olmalıdır");

            RuleFor(x => x.Stage)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Prospekt", "Teklif", "Pazarlık", "Kapandı-Kazandı", "Kapandı-Kaybetti" }.Contains(s))
                .WithMessage("Geçersiz aşama değeri");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("Başlangıç tarihi, bitiş tarihinden büyük olamaz");
        }
    }
}
