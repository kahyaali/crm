using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CrmApi.Hubs
{
    [Authorize]
    public class NotificationHub:Hub
    {
        private static readonly Dictionary<string, string> _userConnections = new();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections[userId] = Context.ConnectionId;
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.Remove(userId);         
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotificationToUser(int userId, string title, string message)
        {
            if (_userConnections.TryGetValue(userId.ToString(), out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveNotification", new
                {
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public async Task SendNotificationToAll(string title, string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task JoinUploadGroup(string uploadId)
        {
            if (string.IsNullOrEmpty(uploadId)) return;
            await Groups.AddToGroupAsync(Context.ConnectionId, uploadId);
        }

        public async Task LeaveUploadGroup(string uploadId)
        {
            if (string.IsNullOrEmpty(uploadId)) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, uploadId);
        }

        //  Progress bildirimi gönderme 
        public async Task SendUploadProgress(string uploadId, object progressData)
        {
            await Clients.Group(uploadId).SendAsync("ReceiveProgress", progressData);
        }
    }
}
