using Crm.Application.DTOs.Brand;
using FluentValidation;

namespace CrmApi.Validators.BrandValidator
{
    public class UpdateBrandDtoValidator : AbstractValidator<UpdateBrandDto>
    {
        public UpdateBrandDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Geçersiz marka ID");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Marka adı zorunludur")
                .MaximumLength(100).WithMessage("Marka adı en fazla 100 karakter olabilir")
                .MinimumLength(2).WithMessage("Marka adı en az 2 karakter olmalıdır")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$").WithMessage("Marka adı sadece harf içerebilir");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.LogoUrl)
                .MaximumLength(500).WithMessage("Logo URL en fazla 500 karakter olabilir")
                .Must(url => url == null || url.StartsWith("http") || url.StartsWith("/"))
                .WithMessage("Geçerli bir URL giriniz")
                .When(x => !string.IsNullOrEmpty(x.LogoUrl));

            RuleFor(x => x.IsActive)
                .NotNull().WithMessage("Aktiflik durumu belirtilmelidir");
        }
    }
}
