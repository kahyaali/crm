using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Crm.Infrastructure.Services;

namespace Crm.API.Attributes
{
    public class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _permission;

        public HasPermissionAttribute(string permission)
        {
            _permission = permission;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            try
            {
                var permissionService = context.HttpContext.RequestServices.GetService<IPermissionService>();
                if (permissionService == null)
                {
                    context.Result = new ForbidResult();
                    return;
                }

                // ✅ Doğru claim: nameidentifier
                var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var userId = int.Parse(userIdClaim);
                var hasPermission = await permissionService.HasPermissionAsync(userId, _permission);

                if (!hasPermission)
                {
                    context.Result = new ForbidResult();
                }
            }
            catch (Exception)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}