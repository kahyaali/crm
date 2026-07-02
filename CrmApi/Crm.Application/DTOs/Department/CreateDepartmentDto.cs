using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Department
{
    public class CreateDepartmentDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}
