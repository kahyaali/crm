using Crm.Application.DTOs.Notification;
using FluentValidation;

namespace CrmApi.Validators.NotificationValidator
{
    public class CreateNotificationValidator : AbstractValidator<CreateNotificationDto>
    {
        public CreateNotificationValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Başlık zorunludur")
                .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Mesaj zorunludur")
                .MaximumLength(1000).WithMessage("Mesaj en fazla 1000 karakter olabilir");

            RuleFor(x => x.Type)
                .MaximumLength(50).WithMessage("Tip en fazla 50 karakter olabilir")
                .Must(t => string.IsNullOrEmpty(t) || new[] { "Task", "Meeting", "Ticket", "System", "Lead", "Order" }.Contains(t))
                .When(x => !string.IsNullOrEmpty(x.Type))
                .WithMessage("Geçersiz bildirim tipi");

            RuleFor(x => x.RelatedEntityType)
                .MaximumLength(50).WithMessage("Entity tipi en fazla 50 karakter olabilir");
        }
    }
}
