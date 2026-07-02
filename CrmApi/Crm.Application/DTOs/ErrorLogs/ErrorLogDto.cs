using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ErrorLogs
{
    public class ErrorLogDto
    {
        public int Id { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestMethod { get; set; }
        public string? RequestBody { get; set; }
        public string? IpAddress { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? ErrorLevel { get; set; } // Critical, Error, Warning
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
