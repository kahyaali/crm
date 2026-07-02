using Crm.Application.DTOs.ProductCategory;
using FluentValidation;

namespace CrmApi.Validators.ProductCategoryValidator
{
    public class CreateProductCategoryDtoValidator : AbstractValidator<CreateProductCategoryDto>
    {
        public CreateProductCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Kategori adı zorunludur")
                .MaximumLength(100).WithMessage("Kategori adı en fazla 100 karakter olabilir")
                .MinimumLength(2).WithMessage("Kategori adı en az 2 karakter olmalıdır")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$").WithMessage("Kategori adı sadece harf içerebilir");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.ParentCategoryId)
                .GreaterThan(0).WithMessage("Geçersiz üst kategori ID")
                .When(x => x.ParentCategoryId.HasValue);

            RuleFor(x => x.IsActive)
                .NotNull().WithMessage("Aktiflik durumu belirtilmelidir");
        }
    }
}
