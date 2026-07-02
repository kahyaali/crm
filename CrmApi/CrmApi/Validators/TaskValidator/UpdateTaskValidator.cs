using Crm.Application.DTOs.Task;
using FluentValidation;

namespace CrmApi.Validators.TaskValidator
{
    public class UpdateTaskValidator : AbstractValidator<UpdateTaskDto>
    {
        public UpdateTaskValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Geçersiz görev ID");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Görev başlığı zorunludur")
                .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir");

            RuleFor(x => x.Status)
                .Must(s => new[] { "Yeni", "Devam Ediyor", "Tamamlandı", "İptal" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");

            RuleFor(x => x.Priority)
                .Must(s => new[] { "Düşük", "Orta", "Yüksek", "Acil" }.Contains(s))
                .WithMessage("Geçersiz öncelik değeri");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.Now)
                .When(x => x.DueDate.HasValue)
                .WithMessage("Bitiş tarihi bugünden sonra olmalıdır");
        }
    }
}
