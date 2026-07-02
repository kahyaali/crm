using FluentValidation;
using Crm.Domain.Entities;

namespace CrmApi.Validators.DepartmentValidator
{
    public class CreateDepartmentValidator : AbstractValidator<Department>
    {
        public CreateDepartmentValidator()
        {
            RuleFor(x => x.Name)
    .NotEmpty().WithMessage("Departman adı zorunludur")
    .MaximumLength(100).WithMessage("Departman adı en fazla 100 karakter olabilir")
    .MinimumLength(2).WithMessage("Departman adı en az 2 karakter olmalıdır");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }

    public class UpdateDepartmentValidator : AbstractValidator<Department>
    {
        public UpdateDepartmentValidator()
        {
            RuleFor(x => x.Id)
                 .GreaterThan(0).WithMessage("Geçersiz departman ID");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Departman adı zorunludur")
                .MaximumLength(100).WithMessage("Departman adı en fazla 100 karakter olabilir")
                .MinimumLength(2).WithMessage("Departman adı en az 2 karakter olmalıdır");
             

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}
