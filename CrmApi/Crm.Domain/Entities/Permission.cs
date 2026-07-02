using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Permission:BaseEntity
    {
        public string Name { get; set; }  // "customer.view", "customer.create"
        public string Module { get; set; } // "Customers", "Products", "Orders"
        public string Action { get; set; } // "View", "Create", "Edit", "Delete"
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
    }
}
