using Crm.Application.DTOs.Quote;
using FluentValidation;

namespace CrmApi.Validators.QuoteValidator
{
    public class CreateQuoteValidator : AbstractValidator<CreateQuoteDto>
    {
        public CreateQuoteValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0).WithMessage("Geçersiz müşteri ID");

            RuleFor(x => x.QuoteDate)
                .NotEmpty().WithMessage("Teklif tarihi zorunludur");

            RuleFor(x => x.ValidUntil)
                .NotEmpty().WithMessage("Geçerlilik tarihi zorunludur")
                .GreaterThan(x => x.QuoteDate)
                .WithMessage("Geçerlilik tarihi, teklif tarihinden sonra olmalıdır");

            RuleFor(x => x.TaxRate)
                .InclusiveBetween(0, 100).WithMessage("KDV oranı 0-100 arasında olmalıdır");

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Taslak", "Gönderildi", "Onaylandı", "Reddedildi", "İptal" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("En az bir ürün eklemelisiniz");

            RuleForEach(x => x.Items).SetValidator(new CreateQuoteItemValidator());
        }
    }
}
