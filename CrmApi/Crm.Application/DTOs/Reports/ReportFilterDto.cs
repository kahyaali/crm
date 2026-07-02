using Crm.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Reports
{
    public class ReportFilterDto
    {
        public ReportType Type { get; set; }

        // Tarih Aralığı
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Entity Bazlı Filtreler
        public int? PersonelId { get; set; }
        public int? DepartmentId { get; set; }
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? CampaignId { get; set; }

        // Durum Filtreleri
        public string? Status { get; set; }          // Ticket.Status, Lead.Status, vs.
        public string? Priority { get; set; }        // Ticket.Priority
        public string? Category { get; set; }        // Ticket.Category
        public string? Stage { get; set; }           // Opportunity.Stage
        public string? Source { get; set; }          // Lead.Source

        // Para Birimi
        public string? Currency { get; set; } = "TRY";

        // Gruplama
        public string? GroupBy { get; set; }         // "Day", "Week", "Month", "Year"

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = false;
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeSummary { get; set; } = true;
        public string? SearchTerm { get; set; }
    }
}
