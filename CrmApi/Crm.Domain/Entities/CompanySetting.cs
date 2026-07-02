using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class CompanySetting:BaseEntity
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string? Description { get; set; }
    }
}
