using Crm.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public interface IEmailService
    {
        // Mail ayarları
        Task<MailSetting> GetMailSettingsAsync();
        Task UpdateMailSettingsAsync(MailSetting settings);

        // Mail gönderme
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        Task<bool> SendTestEmailAsync(string toEmail);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken);
    }
}
