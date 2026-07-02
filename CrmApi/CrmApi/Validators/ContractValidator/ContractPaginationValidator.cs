using Crm.Application.DTOs.Contract;
using FluentValidation;

namespace CrmApi.Validators.ContractValidator
{
    public class ContractPaginationValidator : AbstractValidator<ContractPaginationDto>
    {
        public ContractPaginationValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası en az 1 olmalıdır");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1-100 arasında olmalıdır");

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Taslak", "Bekliyor", "Aktif", "Süresi Doldu", "Feshedildi" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("Başlangıç tarihi, bitiş tarihinden büyük olamaz");
        }
    }
}
