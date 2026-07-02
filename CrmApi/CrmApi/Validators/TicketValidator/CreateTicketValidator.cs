using Crm.Application.DTOs.Ticket;
using FluentValidation;

namespace CrmApi.Validators.TicketValidator
{
    public class CreateTicketValidator : AbstractValidator<CreateTicketDto>
    {
        public CreateTicketValidator()
        {
            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Konu boş olamaz")
                .MaximumLength(200).WithMessage("Konu en fazla 200 karakter olabilir");

            RuleFor(x => x.CustomerId)
                .GreaterThan(0).WithMessage("Geçerli bir müşteri seçiniz");

            RuleFor(x => x.Priority)
                .Must(p => string.IsNullOrEmpty(p) || new[] { "Low", "Medium", "High", "Critical" }.Contains(p))
                .WithMessage("Geçersiz öncelik değeri");

            RuleFor(x => x.Category)
                .Must(c => string.IsNullOrEmpty(c) || new[] { "Complaint", "Request", "Information", "Technical" }.Contains(c))
                .WithMessage("Geçersiz kategori");
        }
    }
}
