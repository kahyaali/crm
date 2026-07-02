using FluentValidation;
using Crm.Domain.Entities;

namespace CrmApi.Validators.PositionValidator
{
    public class CreatePositionValidator : AbstractValidator<Position>
    {
        public CreatePositionValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Pozisyon adı zorunludur")
                .MaximumLength(100).WithMessage("Pozisyon adı en fazla 100 karakter olabilir")
                .MinimumLength(2).WithMessage("Pozisyon adı en az 2 karakter olmalıdır");
              

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }

    public class UpdatePositionValidator : AbstractValidator<Position>
    {
        public UpdatePositionValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Geçersiz pozisyon ID");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Pozisyon adı zorunludur")
                .MaximumLength(100).WithMessage("Pozisyon adı en fazla 100 karakter olabilir")
                .MinimumLength(2).WithMessage("Pozisyon adı en az 2 karakter olmalıdır");
              

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}
