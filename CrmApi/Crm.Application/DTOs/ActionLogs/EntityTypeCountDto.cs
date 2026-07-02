using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ActionLogs
{
    public class EntityTypeCountDto
    {
        public string EntityType { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
