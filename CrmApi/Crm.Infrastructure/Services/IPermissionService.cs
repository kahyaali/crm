using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(int userId, string permissionName);
        Task<List<string>> GetUserPermissionsAsync(int userId);
    }
}
