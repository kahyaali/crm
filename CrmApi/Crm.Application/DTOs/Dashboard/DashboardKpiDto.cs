using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Dashboard
{
    public class DashboardKpiDto
    {
        // Ticket KPI
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }

        // Lead KPI
        public int TotalLeads { get; set; }
        public int NewLeads { get; set; }
        public int QualifiedLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int LostLeads { get; set; }
        public decimal ConversionRate { get; set; }  // Yüzde

        // Finansal KPI
        public decimal TotalRevenue { get; set; }     // Toplam gelir (Invoice)
        public decimal TotalPaid { get; set; }        // Ödenen miktar
        public decimal TotalReceivable { get; set; }  // Alacak
        public decimal AverageOrderValue { get; set; } // Ortalama sipariş

        public int TotalOrders { get; set; }

        // Opportunity KPI
        public int TotalOpportunities { get; set; }
        public decimal TotalOpportunityValue { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal WinRate { get; set; }          // Kazanma oranı

        // Personel KPI
        public int TotalPersonnel { get; set; }
        public int ActivePersonnel { get; set; }

        // Müşteri KPI
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
    }
}
