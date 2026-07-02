using Crm.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public interface IDataFilterService
    {

        // personeller kendilerine bağlı personel ve müşterileri görebilmesi için 
        Task<int> GetCurrentUserId();
        Task<Personel?> GetCurrentPersonel();
        Task<bool> IsAdmin();
        Task<IQueryable<Personel>> FilterPersonelsByRole(IQueryable<Personel> query);
        Task<IQueryable<Customer>> FilterCustomersByRole(IQueryable<Customer> query);
        Task<IQueryable<Lead>> FilterLeadsByRole(IQueryable<Lead> query);
    }
}
