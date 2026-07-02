using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class ErrorLog : BaseEntity
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestMethod { get; set; }
        public string? RequestBody { get; set; }
        public string? IpAddress { get; set; }
        public int? UserId { get; set; }
        public virtual User? User { get; set; }
        public string? ErrorLevel { get; set; } // Critical, Error, Warning
        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNote { get; set; }
    }
}
