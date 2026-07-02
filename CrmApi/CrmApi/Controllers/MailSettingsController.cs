using Crm.Domain.Entities;
using Crm.Infrastructure.Services;
using MailKit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MailSettingsController : ControllerBase
    {
        private readonly IEmailService _emailService;  

        public MailSettingsController(IEmailService emailService)  
        {
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _emailService.GetMailSettingsAsync();
            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] MailSetting settings)
        {
            await _emailService.UpdateMailSettingsAsync(settings);
            return Ok(new { message = "Mail ayarları güncellendi" });
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
        {
            var result = await _emailService.SendTestEmailAsync(request.Email);
            if (result)
                return Ok(new { message = "Test maili gönderildi" });
            else
                return BadRequest(new { message = "Mail gönderilemedi, ayarları kontrol edin" });
        }
    }

    public class TestEmailRequest
    {
        public string Email { get; set; }
    }
}
