using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ErrorLogs
{
    public class ErrorLevelCountDto
    {
        public string ErrorLevel { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
