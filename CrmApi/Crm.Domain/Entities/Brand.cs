using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Brand:BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
