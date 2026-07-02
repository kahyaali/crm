using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Enums
{
    public enum ReportType
    {
        // Ticket Raporları (1-9)
        TicketStatusReport = 1,
        TicketPerformanceReport = 2,
        TicketCategoryReport = 3,
        TicketPriorityReport = 4,

        // Lead Raporları (10-19)
        LeadStatusReport = 10,
        LeadConversionReport = 11,
        LeadSourceReport = 12,

        // Satış Raporları (20-29)
        OpportunityReport = 20,
        RevenueReport = 21,

        // Müşteri Raporları (30-39)
        CustomerReport = 30,
        CustomerActivityReport = 31,

        // Personel Raporları (40-49)
        PersonnelPerformanceReport = 40,

        // Finansal Raporlar (50-59)
        FinancialReport = 50,

        // Kampanya Raporları (60-69)
        CampaignReport = 60,

        DashboardKPI = 99  
    }
}