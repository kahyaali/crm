using Crm.Application.DTOs.Invoice;
using FluentValidation;

namespace CrmApi.Validators.InvoiceValidator
{
    public class UpdateInvoiceItemValidator : AbstractValidator<UpdateInvoiceItemDto>
    {
        public UpdateInvoiceItemValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("Geçersiz ürün ID");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Adet 0'dan büyük olmalıdır");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Birim fiyat 0 veya daha büyük olmalıdır");
        }
    }
}
