using Crm.Application.DTOs.Common;
using Crm.Application.DTOs.Dashboard;
using Crm.Application.DTOs.Reports;
using Crm.Domain.Enums;
using Crm.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IReportService _reportService;

        public DashboardController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var data = await _reportService.GetDashboardDataAsync();
                return Ok(new ApiResponse<DashboardDto>
                {
                    Success = true,
                    Data = data,
                    Message = "Dashboard verileri başarıyla getirildi"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Dashboard verileri alınamadı: {ex.Message}"
                });
            }
        }

        [HttpPost("export/excel")]
        public async Task<IActionResult> ExportExcel([FromBody] ExportFilterDto filter)
        {
            try
            {
                if (filter == null)
                {
                    filter = new ExportFilterDto();
                }


                var reportFilter = new ReportFilterDto
                {
                    StartDate = filter.StartDate,
                    EndDate = filter.EndDate,
                    Type = ReportType.DashboardKPI
                };

                var fileBytes = await _reportService.ExportToExcelAsync(reportFilter);
                var fileName = $"Dashboard_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Excel dışa aktarılamadı: {ex.Message}"
                });
            }
        }

        [HttpPost("export/pdf")]
        public async Task<IActionResult> ExportPdf([FromBody] ExportFilterDto filter)
        {
            try
            {
                if (filter == null)
                {
                    filter = new ExportFilterDto();
                }

                //  Type'ı string olarak gönder
                var reportFilter = new ReportFilterDto
                {
                    StartDate = filter.StartDate,
                    EndDate = filter.EndDate,
                    Type = ReportType.DashboardKPI
                };

                var fileBytes = await _reportService.ExportToPdfAsync(reportFilter);
                var fileName = $"Dashboard_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(
                    fileBytes,
                    "application/pdf",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = $"PDF dışa aktarılamadı: {ex.Message}"
                });
            }
        }
    }
}
