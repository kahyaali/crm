using Crm.Application.DTOs.Contract;
using FluentValidation;

namespace CrmApi.Validators.ContractValidator
{
    public class CreateContractValidator : AbstractValidator<CreateContractDto>
    {
        public CreateContractValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0).WithMessage("Geçersiz müşteri ID");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Sözleşme başlığı zorunludur")
                .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Başlangıç tarihi zorunludur");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("Bitiş tarihi zorunludur")
                .GreaterThan(x => x.StartDate)
                .WithMessage("Bitiş tarihi, başlangıç tarihinden sonra olmalıdır");

            RuleFor(x => x.ContractValue)
                .GreaterThanOrEqualTo(0).WithMessage("Sözleşme değeri 0 veya daha büyük olmalıdır");

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Taslak", "Bekliyor", "Aktif", "Süresi Doldu", "Feshedildi" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");
        }
    }
}
