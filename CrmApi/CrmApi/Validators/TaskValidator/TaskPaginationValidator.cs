using Crm.Application.DTOs.Task;
using FluentValidation;

namespace CrmApi.Validators.TaskValidator
{
    public class TaskPaginationValidator : AbstractValidator<TaskPaginationDto>
    {
        public TaskPaginationValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası en az 1 olmalıdır");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1-100 arasında olmalıdır");

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Yeni", "Devam Ediyor", "Tamamlandı", "İptal" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");

            RuleFor(x => x.Priority)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Düşük", "Orta", "Yüksek", "Acil" }.Contains(s))
                .WithMessage("Geçersiz öncelik değeri");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("Başlangıç tarihi, bitiş tarihinden büyük olamaz");
        }
    }
}
