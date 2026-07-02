


using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories;
using CrmApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IUnitOfWork unitOfWork, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        public async Task SendToUserAsync(int userId, string title, string message)
        {
            var notification = new Notification
            {
                PersonelId = userId,
                Title = title,
                Message = message,
                Type = "System",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task SendToAllAsync(string title, string message)
        {
            var personels = await _unitOfWork.Query<Personel>()
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var personelId in personels)
            {
                var notification = new Notification
                {
                    PersonelId = personelId,
                    Title = title,
                    Message = message,
                    Type = "System",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _unitOfWork.AddAsync(notification);
            }
            await _unitOfWork.CompleteAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task SendToPersonelAsync(int personelId, string title, string message, string type = "System", int? relatedEntityId = null, string? relatedEntityType = null)
        {
            var notification = new Notification
            {
                PersonelId = personelId,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            await _hubContext.Clients.User(personelId.ToString()).SendAsync("ReceiveNotification", new
            {
                Type = type,
                Title = title,
                Message = message,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task SendToAdminsAsync(string title, string message, string type = "System", int? relatedEntityId = null, string? relatedEntityType = null)
        {
            var admins = await _unitOfWork.Query<Personel>()
                .Include(p => p.User)
                .ThenInclude(u => u.Role)
                .Where(p => p.User != null && p.User.Role != null && (p.User.Role.Name == "SystemAdmin" || p.User.Role.Name == "Admin"))
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var adminId in admins)
            {
                await SendToPersonelAsync(adminId, title, message, type, relatedEntityId, relatedEntityType);
            }
        }

      
        public async Task SendUploadProgressAsync<T>(T progress) where T : class
        {
            try
            {
                var uploadId = GetUploadIdFromProgress(progress);

                if (string.IsNullOrEmpty(uploadId))
                {
                    Console.WriteLine("⚠️ UploadId BOŞ! Tüm client'lara gönderiliyor...");
                    await _hubContext.Clients.All.SendAsync("ReceiveProgress", progress);
                    Console.WriteLine($"✅ PROGRESS GÖNDERİLDİ (Tüm Clientlar)");
                }
                else
                {
                    Console.WriteLine($"📤 PROGRESS GÖNDERİLİYOR: UploadId={uploadId}");
                    await _hubContext.Clients.Group(uploadId).SendAsync("ReceiveProgress", progress);
                    Console.WriteLine($"✅ PROGRESS GÖNDERİLDİ: {uploadId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ PROGRESS HATASI: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }
        }

        private string GetUploadIdFromProgress<T>(T progress) where T : class
        {
            var property = typeof(T).GetProperty("UploadId");
            if (property != null)
            {
                var value = property.GetValue(progress);
                return value?.ToString();
            }
            return null;
        }
    }
}