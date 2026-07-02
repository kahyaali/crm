using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Role:BaseEntity
    {
        public string Name { get; set; }  // "Satış Müdürü", "Muhasebeci"
        public string? Description { get; set; }
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
    }
}
