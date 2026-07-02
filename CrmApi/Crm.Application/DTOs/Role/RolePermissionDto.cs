using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Role
{
    public class RolePermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Module { get; set; }
        public string Action { get; set; }
    }
}
