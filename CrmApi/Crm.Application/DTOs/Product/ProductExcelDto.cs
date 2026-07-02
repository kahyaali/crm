using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Product
{
    public class ProductExcelDto
    {
        public string Name { get; set; }
        public string? Sku { get; set; }
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int? StockQuantity { get; set; }
        public int? MinStockLevel { get; set; }
        public int? MaxStockLevel { get; set; }
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public string? Unit { get; set; }
        public string? IsActive { get; set; }
        public string? IsStockTrackable { get; set; }
    }
}
