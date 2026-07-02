namespace Crm.Application.DTOs.Product
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Sku { get; set; }
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; }
        public int StockQuantity { get; set; }
        public int? MinStockLevel { get; set; }
        public int? MaxStockLevel { get; set; }
        public int? CategoryId { get; set; } 
        public string? CategoryName { get; set; }
        public int? BrandId { get; set; } 
        public string? BrandName { get; set; }
        public string? Unit { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsStockTrackable { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
