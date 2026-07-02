using Crm.Application.DTOs.Product;
using FluentValidation;

namespace CrmApi.Validators.ProductValidator
{
    public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductDtoValidator()
        {
            // ID kontrolü
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Geçersiz ürün ID");

            // Ürün Adı
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ürün adı zorunludur")
                .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir")
                .MinimumLength(2).WithMessage("Ürün adı en az 2 karakter olmalıdır");

            // SKU (Opsiyonel - varsa kontrol et)
            RuleFor(x => x.Sku)
                .MaximumLength(50).WithMessage("SKU en fazla 50 karakter olabilir")
                .Matches(@"^[A-Za-z0-9-_]+$").WithMessage("SKU sadece harf, rakam, - ve _ içerebilir")
                .When(x => !string.IsNullOrEmpty(x.Sku));

            // Barkod (Opsiyonel)
            RuleFor(x => x.Barcode)
                .MaximumLength(50).WithMessage("Barkod en fazla 50 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Barcode));

            // Açıklama (Opsiyonel)
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Açıklama en fazla 1000 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Description));

            // Fiyat
            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Fiyat 0'dan büyük olmalıdır")
                .LessThanOrEqualTo(10000000).WithMessage("Fiyat çok yüksek");

            // Para Birimi
            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Para birimi zorunludur")
                .Must(x => x == "TRY" || x == "USD" || x == "EUR" || x == "GBP")
                .WithMessage("Para birimi TRY, USD, EUR veya GBP olmalıdır");

            // Stok Miktarı (Stok takibi aktifse kontrol et)
            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stok miktarı 0'dan küçük olamaz")
                .When(x => x.IsStockTrackable);

            // Min/Max Stok Seviyesi (Opsiyonel)
            RuleFor(x => x.MinStockLevel)
                .GreaterThanOrEqualTo(0).WithMessage("Min. stok seviyesi 0'dan küçük olamaz")
                .When(x => x.MinStockLevel.HasValue);

            RuleFor(x => x.MaxStockLevel)
                .GreaterThanOrEqualTo(0).WithMessage("Max. stok seviyesi 0'dan küçük olamaz")
                .When(x => x.MaxStockLevel.HasValue);

            // Min < Max kontrolü
            RuleFor(x => x)
                .Must(x => !x.MinStockLevel.HasValue || !x.MaxStockLevel.HasValue || x.MinStockLevel <= x.MaxStockLevel)
                .WithMessage("Min. stok seviyesi, Max. stok seviyesinden büyük olamaz");

            // Kategori (Opsiyonel)
            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("Geçerli bir kategori seçiniz")
                .When(x => x.CategoryId.HasValue);

            // Marka (Opsiyonel)
            RuleFor(x => x.BrandId)
                .GreaterThan(0).WithMessage("Geçerli bir marka seçiniz")
                .When(x => x.BrandId.HasValue);

            // Birim
            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("Birim alanı zorunludur")
                .MaximumLength(20).WithMessage("Birim en fazla 20 karakter olabilir");

            // Aktiflik
            RuleFor(x => x.IsActive)
                .NotNull().WithMessage("Aktiflik durumu belirtilmelidir");

            // Stok Takibi
            RuleFor(x => x.IsStockTrackable)
                .NotNull().WithMessage("Stok takibi durumu belirtilmelidir");
        }
    }
}
