using Crm.Application.DTOs.Lead;
using FluentValidation;

namespace CrmApi.Validators.LeadValidator
{
    public class ConvertLeadToCustomerValidator : AbstractValidator<ConvertLeadToCustomerDto>
    {
        public ConvertLeadToCustomerValidator()
        {
          
            RuleFor(x => x.TaxNumber)
                .MaximumLength(20).WithMessage("Vergi no en fazla 20 karakter olabilir");

            RuleFor(x => x.TaxOffice)
                .MaximumLength(100).WithMessage("Vergi dairesi en fazla 100 karakter olabilir");

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olabilir");

            RuleFor(x => x.City)
                .MaximumLength(50).WithMessage("Şehir en fazla 50 karakter olabilir");

            RuleFor(x => x.District)
                .MaximumLength(50).WithMessage("İlçe en fazla 50 karakter olabilir");
        }
    }
}
