using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Position:BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Personel> Personels { get; set; } = new List<Personel>();
    }
}
