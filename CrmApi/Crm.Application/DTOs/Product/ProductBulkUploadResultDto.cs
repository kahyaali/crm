using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Product
{
    public class ProductBulkUploadResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ProductBulkUploadErrorDto> Errors { get; set; } = new();
        public List<ProductDto> CreatedProducts { get; set; } = new();
    }

    public class ProductBulkUploadErrorDto
    {
        public int RowNumber { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public string ErrorMessage { get; set; }
    }
}
