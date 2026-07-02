using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ActionLogs
{
    public class CreateActionLogDto
    {
        public string ActionType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? OldData { get; set; }
        public string? NewData { get; set; }
        public string? AdditionalInfo { get; set; }
    }
}
