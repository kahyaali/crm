using Crm.Application.DTOs.Payment;
using FluentValidation;

namespace CrmApi.Validators.PaymentValidator
{
    public class AddPaymentValidator : AbstractValidator<AddPaymentDto>
    {
        public AddPaymentValidator()
        {
            RuleFor(x => x.InvoiceId)
                .GreaterThan(0).WithMessage("Geçersiz fatura ID");

            RuleFor(x => x.PaymentDate)
                .NotEmpty().WithMessage("Ödeme tarihi zorunludur");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Ödeme tutarı 0'dan büyük olmalıdır");

            RuleFor(x => x.PaymentMethod)
                .MaximumLength(50).WithMessage("Ödeme yöntemi en fazla 50 karakter olabilir");
        }
    }
}
