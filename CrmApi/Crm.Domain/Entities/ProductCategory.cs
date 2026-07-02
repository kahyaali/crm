using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class ProductCategory : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public virtual ProductCategory? ParentCategory { get; set; }
        public virtual ICollection<ProductCategory> SubCategories { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}
