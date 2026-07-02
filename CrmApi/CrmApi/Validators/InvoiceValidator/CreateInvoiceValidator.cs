using Crm.Application.DTOs.Invoice;
using FluentValidation;

namespace CrmApi.Validators.InvoiceValidator
{
    public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceDto>
    {
        public CreateInvoiceValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0).WithMessage("Geçersiz müşteri ID");

            RuleFor(x => x.InvoiceDate)
                .NotEmpty().WithMessage("Fatura tarihi zorunludur");

            RuleFor(x => x.DueDate)
                .NotEmpty().WithMessage("Son ödeme tarihi zorunludur")
                .GreaterThan(x => x.InvoiceDate)
                .WithMessage("Son ödeme tarihi, fatura tarihinden sonra olmalıdır");

            RuleFor(x => x.TaxRate)
                .InclusiveBetween(0, 100).WithMessage("KDV oranı 0-100 arasında olmalıdır");

            RuleFor(x => x.Status)
                .Must(s => new[] { "Gönderildi", "Kısmen Ödendi", "Ödendi", "Gecikmiş", "İptal" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("En az bir ürün eklemelisiniz");

            RuleForEach(x => x.Items).SetValidator(new CreateInvoiceItemValidator());
        }
    }
}
