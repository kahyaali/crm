using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ProductCategory
{
    public class ProductCategoryPaginationDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int? ParentCategoryId { get; set; }
    }
}
