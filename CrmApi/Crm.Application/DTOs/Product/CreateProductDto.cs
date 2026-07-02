namespace Crm.Application.DTOs.Product
{
    public class CreateProductDto
    {
        public string Name { get; set; }
        public string? Sku { get; set; }
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; } = "TRY";
        public int StockQuantity { get; set; }
        public int? MinStockLevel { get; set; }
        public int? MaxStockLevel { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public string? Brand { get; set; }
        public string? Unit { get; set; } = "Adet";
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsStockTrackable { get; set; } = true;
    }
}
