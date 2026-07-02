using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ErrorLogs
{
    public class DailyErrorCountDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
