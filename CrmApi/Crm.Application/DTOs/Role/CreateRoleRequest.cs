using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Role
{
    public class CreateRoleRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}
