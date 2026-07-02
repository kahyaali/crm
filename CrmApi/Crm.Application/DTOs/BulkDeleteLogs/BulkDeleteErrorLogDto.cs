using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.BulkDeleteLogs
{
    public class BulkDeleteErrorLogDto
    {
        public List<int> Ids { get; set; } = new();
        public bool HardDelete { get; set; } = false;
    }
}
