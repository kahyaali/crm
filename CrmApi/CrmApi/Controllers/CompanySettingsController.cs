using Crm.API.Attributes;
using Crm.Domain.Entities;
using Crm.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CompanySettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanySettingsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("logo")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLogo()
        {
            var setting = await _context.CompanySettings
                .FirstOrDefaultAsync(s => s.Key == "CompanyLogo");

            var logoUrl = setting?.Value ?? "/logos/default-logo.png";
            return Ok(new { logoUrl });
        }

        [HttpPost("logo")]
        [HasPermission("settings.manage")]
        public async Task<IActionResult> UploadLogo(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = "Geçersiz dosya formatı" });

            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new { message = "Dosya boyutu 2MB'dan küçük olmalıdır" });

            var fileName = $"logo_{DateTime.Now.Ticks}{extension}";
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logos");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var logoUrl = $"/logos/{fileName}";

            var existing = await _context.CompanySettings
                .FirstOrDefaultAsync(s => s.Key == "CompanyLogo");

            if (existing != null)
            {
                // Eski dosyayı sil
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existing.Value.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);

                existing.Value = logoUrl;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                await _context.CompanySettings.AddAsync(new CompanySetting
                {
                    Key = "CompanyLogo",
                    Value = logoUrl,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { logoUrl });
        }

  
        [HttpDelete("logo")]
        [HasPermission("settings.manage")]
        public async Task<IActionResult> DeleteLogo()
        {
            var setting = await _context.CompanySettings
                .FirstOrDefaultAsync(s => s.Key == "CompanyLogo");

            if (setting != null)
            {
                // Dosyayı sil
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", setting.Value.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                _context.CompanySettings.Remove(setting);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Logo silindi" });
        }
    }
}
