using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Crm.Infrastructure.Helpers
{
    public class HttpContextHelper
    {
        public static int? GetCurrentUserId(HttpContext httpContext)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        public static string GetClientIp(HttpContext httpContext)
        {
            return httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                   ?? httpContext.Connection.RemoteIpAddress?.ToString()
                   ?? "Unknown";
        }

        public static string GetUserAgent(HttpContext httpContext)
        {
            return httpContext.Request.Headers["User-Agent"].ToString();
        }

        public static string GetCurrentUserEmail(HttpContext httpContext)
        {
            return httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
        }

        public static bool IsAuthenticated(HttpContext httpContext)
        {
            return httpContext.User.Identity?.IsAuthenticated ?? false;
        }
    }
}
