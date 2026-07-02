using Crm.Domain.Entities;
using Crm.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Crm.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public EmailService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ========== MAIL AYARLARI ==========
        public async Task<MailSetting> GetMailSettingsAsync()
        {
            var settings = await _context.MailSettings.FirstOrDefaultAsync(m => m.IsDefault);
            if (settings == null)
            {
                settings = new MailSetting
                {
                    SmtpServer = "smtp.gmail.com",
                    SmtpPort = 587,
                    EnableSsl = true,
                    IsDefault = true
                };
            }
            return settings;
        }

        public async Task UpdateMailSettingsAsync(MailSetting settings)
        {
            var existing = await _context.MailSettings.FirstOrDefaultAsync(m => m.IsDefault);
            if (existing != null)
            {
                existing.SmtpServer = settings.SmtpServer;
                existing.SmtpPort = settings.SmtpPort;
                existing.SmtpUsername = settings.SmtpUsername;
                existing.SmtpPassword = settings.SmtpPassword;
                existing.FromEmail = settings.FromEmail;
                existing.FromName = settings.FromName;
                existing.EnableSsl = settings.EnableSsl;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                settings.IsDefault = true;
                settings.CreatedAt = DateTime.UtcNow;
                await _context.MailSettings.AddAsync(settings);
            }
            await _context.SaveChangesAsync();
        }

        // ========== MAIL GÖNDERME ==========
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var settings = await GetMailSettingsAsync();

                if (string.IsNullOrEmpty(settings.SmtpUsername) || string.IsNullOrEmpty(settings.SmtpPassword))
                {
                    Console.WriteLine("❌ SMTP kullanıcı adı veya şifre eksik!");
                    await LogEmail(toEmail, subject, body, false, "SMTP kullanıcı adı veya şifre eksik");
                    return false;
                }

                using var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort);
                client.EnableSsl = settings.EnableSsl;
                client.Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword);

                var message = new MailMessage
                {
                    From = new MailAddress(settings.FromEmail ?? settings.SmtpUsername, settings.FromName ?? "CRM System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                Console.WriteLine($"✅ Mail gönderildi: {toEmail}");
                await LogEmail(toEmail, subject, body, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Mail hatası: {ex.Message}");
                await LogEmail(toEmail, subject, body, false, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync(string toEmail)
        {
            var body = $@"
                <h2>Test Maili Başarılı!</h2>
                <p>CRM sisteminizden gönderilen bir test mailidir.</p>
                <p><strong>Tarih:</strong> {DateTime.Now}</p>
                <hr/>
                <p>Eğer bu maili aldıysanız, mail ayarlarınız doğru çalışıyor! </p>
            ";
            return await SendEmailAsync(toEmail, "CRM Test Maili", body);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken)
        {
            var appUrl = _configuration["AppUrl"] ?? "http://localhost:5173";
            var resetLink = $"{appUrl}/reset-password?token={resetToken}&email={toEmail}";

            var body = $@"
                      <h2>🔐 Şifre Sıfırlama</h2>
                      <p>Şifrenizi sıfırlamak için aşağıdaki linke tıklayın:</p>
                      <a href='{resetLink}'>{resetLink}</a>
                      <hr/>
                      <p>Bu link <strong>1 saat</strong> geçerlidir.</p>";

            // MailSettings tablosunda kayıt var mı kontrol et
            var settings = await _context.MailSettings.FirstOrDefaultAsync(m => m.IsDefault);

            // EĞER MAIL AYARLARI YOKSA VEYA BOŞSA - FALSE DÖN
            if (settings == null)
            {
                Console.WriteLine("❌ MailSettings tablosunda kayıt yok!");
                await LogEmail(toEmail, "Şifre Sıfırlama - CRM", body, false, "Mail ayarları bulunamadı");
                return false;
            }

            if (string.IsNullOrEmpty(settings.SmtpUsername) || string.IsNullOrEmpty(settings.SmtpPassword))
            {
                Console.WriteLine("❌ SMTP kullanıcı adı veya şifre eksik!");
                await LogEmail(toEmail, "Şifre Sıfırlama - CRM", body, false, "SMTP kullanıcı adı veya şifre eksik");
                return false;
            }

            return await SendEmailAsync(toEmail, "Şifre Sıfırlama - CRM", body);
        }

        // ========== LOG ==========
        private async Task LogEmail(string toEmail, string subject, string body, bool isSent, string errorMessage = null)
        {
            var log = new EmailLog
            {
                ToEmail = toEmail,
                Subject = subject,
                Body = body,
                IsSent = isSent,
                ErrorMessage = errorMessage,
                SentAt = isSent ? DateTime.UtcNow : null
            };
            await _context.EmailLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
    }
}