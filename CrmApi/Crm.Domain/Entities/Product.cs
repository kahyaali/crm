using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; }              // Ürün adı
        public string? Sku { get; set; }              // Stok Kodu (benzersiz)
        public string? Barcode { get; set; }          // Barkod
        public string? Description { get; set; }      // Açıklama

        public decimal Price { get; set; }             // Fiyat
        public string? Currency { get; set; } = "TRY"; // Para birimi (TRY, USD, EUR)

        public int StockQuantity { get; set; }         // Stok miktarı
        public int? MinStockLevel { get; set; }        // Minimum stok seviyesi (uyarı için)
        public int? MaxStockLevel { get; set; }        // Maksimum stok seviyesi

        public int? CategoryId { get; set; }           // Kategori
        public virtual ProductCategory? Category { get; set; }
        public int? BrandId { get; set; }              //  Marka 
        public virtual Brand? Brand { get; set; }
        public string? Unit { get; set; } = "Adet";    // Birim (Adet, Kg, Lt, etc.)

        public string? ImageUrl { get; set; }          // Ürün resmi URL'i
        public bool IsActive { get; set; } = true;     // Aktif/Pasif
        public bool IsStockTrackable { get; set; } = true; // Stok takibi yapılsın mı?

        // İlişkiler
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
