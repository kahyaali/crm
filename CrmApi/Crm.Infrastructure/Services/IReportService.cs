using Crm.Application.DTOs.Dashboard;
using Crm.Application.DTOs.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public interface IReportService
    {
        Task<DashboardDto> GetDashboardDataAsync();
        Task<byte[]> GenerateReportAsync(ReportFilterDto filter);
        Task<byte[]> ExportToExcelAsync(ReportFilterDto filter);
        Task<byte[]> ExportToPdfAsync(ReportFilterDto filter);
        Task<ReportDataDto> GetReportDataAsync(ReportFilterDto filter);
    }
}
