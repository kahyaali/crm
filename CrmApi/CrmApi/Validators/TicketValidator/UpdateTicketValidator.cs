using Crm.Application.DTOs.Ticket;
using FluentValidation;

namespace CrmApi.Validators.TicketValidator
{
    public class UpdateTicketValidator : AbstractValidator<UpdateTicketDto>
    {
        public UpdateTicketValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Geçersiz ticket ID");

            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Konu boş olamaz")
                .MaximumLength(200).WithMessage("Konu en fazla 200 karakter olabilir");

            RuleFor(x => x.Priority)
                .Must(p => string.IsNullOrEmpty(p) || new[] { "Low", "Medium", "High", "Critical" }.Contains(p))
                .WithMessage("Geçersiz öncelik değeri");

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Open", "InProgress", "OnHold", "Resolved", "Closed" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");
        }
    }
}
