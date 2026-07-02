using Crm.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public interface ISystemAdminService
    {
        Task<bool> IsSystemAdmin(int userId);
        Task<bool> CanDeleteUser(int userId);
        Task<bool> CanUpdateUser(int userId, User updatedUser);
    }
}
