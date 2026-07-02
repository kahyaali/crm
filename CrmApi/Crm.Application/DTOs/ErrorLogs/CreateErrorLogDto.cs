using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ErrorLogs
{
    public class CreateErrorLogDto
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestMethod { get; set; }
        public string? RequestBody { get; set; }
        public string? ErrorLevel { get; set; } = "Error";
    }
}
