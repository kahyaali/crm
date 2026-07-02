using Crm.Application.DTOs.Opportunity;
using FluentValidation;

namespace CrmApi.Validators.OpportunityValidator
{
    public class CreateOpportunityValidator : AbstractValidator<CreateOpportunityDto>
    {
        public CreateOpportunityValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Fırsat adı zorunludur")
                .MaximumLength(200).WithMessage("Fırsat adı en fazla 200 karakter olabilir");

            RuleFor(x => x.CustomerId)
                .GreaterThan(0).WithMessage("Geçersiz müşteri ID");

            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(0).WithMessage("Tutar 0 veya daha büyük olmalıdır");

            RuleFor(x => x.Stage)
                .Must(s => new[] { "Prospekt", "Teklif", "Pazarlık", "Kapandı-Kazandı", "Kapandı-Kaybetti" }.Contains(s))
                .WithMessage("Geçersiz aşama değeri");
        }
    }
}
