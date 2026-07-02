using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ActionLogs
{
    public class UserActionCountDto
    {
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public int Count { get; set; }
    }
}
