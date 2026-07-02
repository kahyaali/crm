using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public interface IExchangeRateService
    {
        Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
        Task<decimal> GetRateAsync(string fromCurrency, string toCurrency);

    }
}
