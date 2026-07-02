using Crm.Application.DTOs.Customer;
using FluentValidation;

namespace CrmApi.Validators.CustomerValidator
{
    public class UpdateCustomerDtoValidator : AbstractValidator<UpdateCustomerDto>
    {
        public UpdateCustomerDtoValidator()
        {
            // ID kontrolü
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Geçersiz müşteri ID");

            // Ad - Create'deki kuralları tek tek yaz
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ad alanı zorunludur")
                .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$").WithMessage("Ad sadece harf içerebilir");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Soyad alanı zorunludur")
                .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$").WithMessage("Soyad sadece harf içerebilir");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı zorunludur")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz")
                .MaximumLength(100).WithMessage("Email en fazla 100 karakter olabilir");

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("Telefon en fazla 20 karakter olabilir")
                .Matches(@"^[0-9+\s-]+$").WithMessage("Geçerli bir telefon numarası giriniz")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.CompanyName)
                .MaximumLength(100).WithMessage("Şirket adı en fazla 100 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.CompanyName));

            RuleFor(x => x.TaxNumber)
                .MaximumLength(20).WithMessage("Vergi no en fazla 20 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.TaxNumber));

            RuleFor(x => x.CustomerType)
                .NotEmpty().WithMessage("Müşteri tipi zorunludur")
                .Must(x => x == "Bireysel" || x == "Kurumsal" || x == "Potansiyel")
                .WithMessage("Müşteri tipi 'Bireysel', 'Kurumsal' veya 'Potansiyel' olmalıdır");

       
            RuleFor(x => x.Status)
                .Must(x => x == null || x == "Active" || x == "Passive" || x == "Pending" || x == "Lead" || x == "Lost")
                .WithMessage("Geçersiz durum değeri. Aktif, Pasif, Beklemede, Potansiyel veya Kaybedilen olmalıdır");

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Address));

            RuleFor(x => x.City)
                .MaximumLength(50).WithMessage("Şehir en fazla 50 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.City));

              RuleFor(x => x.District)
                .MaximumLength(100).WithMessage("İlçe en fazla 100 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.District));

            RuleFor(x => x.PostalCode)
                .MaximumLength(20).WithMessage("Posta kodu en fazla 20 karakter olabilir")
                .Matches(@"^\d{0,5}$").WithMessage("Posta kodu sadece rakam içerebilir (max 5 haneli)")
                .When(x => !string.IsNullOrEmpty(x.PostalCode));

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notlar en fazla 1000 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.Notes));




            //  AccountNumber - ZORUNLU (Güncelleme sırasında da değiştirilebilir)
            RuleFor(x => x.AccountNumber)
                .NotEmpty().WithMessage("Cari hesap numarası zorunludur")
                .MaximumLength(50).WithMessage("Cari hesap numarası en fazla 50 karakter olabilir")
                .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("Cari hesap numarası sadece harf, rakam, tire ve alt çizgi içerebilir");

            // PaymentType - Ödeme Tipi
            RuleFor(x => x.PaymentType)
                .MaximumLength(50).WithMessage("Ödeme tipi en fazla 50 karakter olabilir")
                .Must(x => string.IsNullOrEmpty(x) || x == "Cash" || x == "Credit" || x == "Deferred")
                .WithMessage("Ödeme tipi 'Cash', 'Credit' veya 'Deferred' olmalıdır");

            // CreditLimit - Kredi Limiti
            RuleFor(x => x.CreditLimit)
                .GreaterThanOrEqualTo(0).WithMessage("Kredi limiti 0 veya daha büyük olmalıdır")
                .When(x => x.CreditLimit.HasValue);

            // PaymentTermDays - Vade Gün Sayısı
            RuleFor(x => x.PaymentTermDays)
                .InclusiveBetween(1, 360).WithMessage("Vade gün sayısı 1-360 arasında olmalıdır")
                .When(x => x.PaymentTermDays.HasValue);

            // DiscountRate - İndirim Oranı
            RuleFor(x => x.DiscountRate)
                .InclusiveBetween(0, 100).WithMessage("İndirim oranı 0-100 arasında olmalıdır")
                .When(x => x.DiscountRate.HasValue);

            // TaxAdministration - Vergi Dairesi (TaxOffice'den farklı olabilir)
            RuleFor(x => x.TaxAdministration)
                .MaximumLength(100).WithMessage("Vergi dairesi en fazla 100 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.TaxAdministration));

            // Website - Web Sitesi
            RuleFor(x => x.Website)
                .MaximumLength(255).WithMessage("Web sitesi en fazla 255 karakter olabilir")
                .Must(x => string.IsNullOrEmpty(x) || Uri.IsWellFormedUriString(x, UriKind.Absolute))
                .WithMessage("Geçerli bir URL giriniz (örn: https://example.com)");

            // ShippingAddress - Teslimat Adresi
            RuleFor(x => x.ShippingAddress)
                .MaximumLength(500).WithMessage("Teslimat adresi en fazla 500 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.ShippingAddress));

            // InvoiceAddress - Fatura Adresi
            RuleFor(x => x.InvoiceAddress)
                .MaximumLength(500).WithMessage("Fatura adresi en fazla 500 karakter olabilir")
                .When(x => !string.IsNullOrEmpty(x.InvoiceAddress));

            // ContactPerson - İlgili Kişi
            RuleFor(x => x.ContactPerson)
                .MaximumLength(100).WithMessage("İlgili kişi en fazla 100 karakter olabilir")
                .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$").WithMessage("İlgili kişi sadece harf içerebilir")
                .When(x => !string.IsNullOrEmpty(x.ContactPerson));

            // ContactPersonPhone - İlgili Kişi Telefonu
            RuleFor(x => x.ContactPersonPhone)
                .MaximumLength(20).WithMessage("İlgili kişi telefonu en fazla 20 karakter olabilir")
                .Matches(@"^[0-9+\s-]+$").WithMessage("Geçerli bir telefon numarası giriniz")
                .When(x => !string.IsNullOrEmpty(x.ContactPersonPhone));

            // ========== CUSTOMER TYPE'A GÖRE KOŞULLU KURALLAR ==========

            // Kurumsal müşteri ise CompanyName zorunlu
            RuleFor(x => x.CompanyName)
                .NotEmpty().WithMessage("Kurumsal müşteri için şirket adı zorunludur")
                .When(x => x.CustomerType == "Kurumsal");

            // Kurumsal müşteri ise TaxNumber zorunlu
            RuleFor(x => x.TaxNumber)
                .NotEmpty().WithMessage("Kurumsal müşteri için vergi numarası zorunludur")
                .When(x => x.CustomerType == "Kurumsal");

            // Kurumsal müşteri ise TaxOffice zorunlu
            RuleFor(x => x.TaxOffice)
                .NotEmpty().WithMessage("Kurumsal müşteri için vergi dairesi zorunludur")
                .When(x => x.CustomerType == "Kurumsal");

            // Bireysel müşteri ise TaxNumber, TaxOffice, CompanyName boş olmalı
            RuleFor(x => x.TaxNumber)
                .Empty().WithMessage("Bireysel müşteri için vergi numarası girilmemelidir")
                .When(x => x.CustomerType == "Bireysel");

            RuleFor(x => x.TaxOffice)
                .Empty().WithMessage("Bireysel müşteri için vergi dairesi girilmemelidir")
                .When(x => x.CustomerType == "Bireysel");

            RuleFor(x => x.CompanyName)
                .Empty().WithMessage("Bireysel müşteri için şirket adı girilmemelidir")
                .When(x => x.CustomerType == "Bireysel");
        }
    }
}

