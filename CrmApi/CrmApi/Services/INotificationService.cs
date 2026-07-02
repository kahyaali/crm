using Crm.Application.DTOs.Personel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrmApi.Services
{
    public interface INotificationService
    {
        Task SendToUserAsync(int userId, string title, string message);
        Task SendToAllAsync(string title, string message);
        Task SendToPersonelAsync(int personelId, string title, string message, string type = "System", int? relatedEntityId = null, string? relatedEntityType = null);
        Task SendToAdminsAsync(string title, string message, string type = "System", int? relatedEntityId = null, string? relatedEntityType = null);

        Task SendUploadProgressAsync<T>(T progress) where T : class;
    }
}
