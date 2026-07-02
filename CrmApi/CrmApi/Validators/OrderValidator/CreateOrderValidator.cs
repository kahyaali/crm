using Crm.Application.DTOs.Order;
using FluentValidation;

namespace CrmApi.Validators.OrderValidator
{
    public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
    {
        public CreateOrderValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0).WithMessage("Müşteri seçilmelidir");

            RuleFor(x => x.OrderDate)
                .NotEmpty().WithMessage("Sipariş tarihi zorunludur");

            RuleFor(x => x.Status)
                .MaximumLength(50).WithMessage("Durum en fazla 50 karakter olabilir")
                .Must(x => string.IsNullOrEmpty(x) ||
                    new[] { "Pending", "Approved", "Preparing", "Shipped", "Delivered", "Cancelled" }.Contains(x))
                .WithMessage("Geçersiz durum değeri");

            RuleFor(x => x.PaymentStatus)
                .MaximumLength(50).WithMessage("Ödeme durumu en fazla 50 karakter olabilir")
                .Must(x => string.IsNullOrEmpty(x) ||
                    new[] { "Pending", "Partial", "Paid", "Cancelled" }.Contains(x))
                .WithMessage("Geçersiz ödeme durumu");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("En az bir ürün eklenmelidir");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .GreaterThan(0).WithMessage("Geçerli bir ürün seçilmelidir");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalıdır");

                item.RuleFor(i => i.UnitPrice)
                    .GreaterThan(0).WithMessage("Birim fiyat 0'dan büyük olmalıdır");
            });
        }
    }
}
