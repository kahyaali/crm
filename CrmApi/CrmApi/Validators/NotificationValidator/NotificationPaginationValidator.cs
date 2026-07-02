using Crm.Application.DTOs.Notification;
using FluentValidation;

namespace CrmApi.Validators.NotificationValidator
{
    public class NotificationPaginationValidator : AbstractValidator<NotificationPaginationDto>
    {
        public NotificationPaginationValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1'den küçük olamaz");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("Sayfa boyutu 1'den küçük olamaz")
                .LessThanOrEqualTo(100).WithMessage("Sayfa boyutu 100'den büyük olamaz");

            RuleFor(x => x.Type)
                .MaximumLength(50).WithMessage("Tip en fazla 50 karakter olabilir")
                .Must(t => string.IsNullOrEmpty(t) || new[] { "Task", "Meeting", "Ticket", "System", "Lead", "Order" }.Contains(t))
                .When(x => !string.IsNullOrEmpty(x.Type))
                .WithMessage("Geçersiz bildirim tipi");
        }
    }
}
