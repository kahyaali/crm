using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Role
{
    public class AssignPermissionsRequest
    {
        public List<int> PermissionIds { get; set; }
    }
}
