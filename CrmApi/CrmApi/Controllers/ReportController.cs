using Crm.Application.DTOs.Common;
using Crm.Application.DTOs.Reports;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IUnitOfWork _unitOfWork;

        public ReportController(IReportService reportService, IUnitOfWork unitOfWork)
        {
            _reportService = reportService;
            _unitOfWork = unitOfWork;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateReport([FromBody] ReportFilterDto filter)
        {
            try
            {
                var data = await _reportService.GetReportDataAsync(filter);
                return Ok(new ApiResponse<ReportDataDto>
                {
                    Success = true,
                    Data = data,
                    Message = "Rapor başarıyla oluşturuldu"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Rapor oluşturulamadı: {ex.Message}"
                });
            }
        }

        [HttpPost("excel")]
        public async Task<IActionResult> ExportExcel([FromBody] ReportFilterDto filter)
        {
            try
            {
                var fileBytes = await _reportService.ExportToExcelAsync(filter);
                var fileName = $"Rapor_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

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

        [HttpPost("pdf")]
        public async Task<IActionResult> ExportPdf([FromBody] ReportFilterDto filter)
        {
            try
            {
                var fileBytes = await _reportService.ExportToPdfAsync(filter);
                var fileName = $"Rapor_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

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

        // Rapor tiplerine göre endpoint'ler
        [HttpGet("types")]
        public IActionResult GetReportTypes()
        {
            var types = new List<object>
    {
        new { Value = 1, Name = "Ticket Durum Raporu" },
        new { Value = 2, Name = "Personel Performans Raporu" },
        new { Value = 3, Name = "Ticket Kategori Raporu" },
        new { Value = 4, Name = "Ticket Öncelik Raporu" },
        new { Value = 10, Name = "Lead Durum Raporu" },
        new { Value = 11, Name = "Lead Dönüşüm Raporu" },
        new { Value = 12, Name = "Lead Kaynak Raporu" },
        new { Value = 20, Name = "Fırsat Raporu" },
        new { Value = 21, Name = "Gelir Raporu" },
        new { Value = 30, Name = "Müşteri Raporu" },
        new { Value = 40, Name = "Personel Performans Raporu" },
        new { Value = 50, Name = "Finansal Rapor" },
        new { Value = 60, Name = "Kampanya Raporu" }
    };

            return Ok(types);
        }

        [HttpGet("personels")]
        public async Task<IActionResult> GetPersonelList()
        {
            try
            {
                var personels = await _unitOfWork.Query<Personel>()
                    .Where(p => !p.IsDeleted && p.IsActive)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.FirstName + " " + p.LastName
                    })
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return Ok(personels);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Personel listesi alınamadı: {ex.Message}"
                });
            }
        }

        //  MÜŞTERİ LİSTESİ 
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomerList()
        {
            try
            {
                var customers = await _unitOfWork.Query<Customer>()
                    .Where(c => !c.IsDeleted && c.IsActive)
                    .Select(c => new
                    {
                        Id = c.Id,
                        Name = c.FirstName + " " + c.LastName
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Müşteri listesi alınamadı: {ex.Message}"
                });
            }
        }
    }
}
