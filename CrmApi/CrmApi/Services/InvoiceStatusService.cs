using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Services
{
    public class InvoiceStatusService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InvoiceStatusService> _logger;

        public InvoiceStatusService(IServiceProvider serviceProvider, ILogger<InvoiceStatusService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Her gün gece 00:00'da çalış
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1);
                var delay = nextRun - now;

                _logger.LogInformation($"Sonraki çalışma: {nextRun}");
                await Task.Delay(delay, stoppingToken);

                await UpdateOverdueInvoices();
            }
        }

        private async Task UpdateOverdueInvoices()
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var logService = scope.ServiceProvider.GetRequiredService<ILogService>();

            try
            {
                // Süresi geçmiş ve tam ödenmemiş faturalar
                var overdueInvoices = await unitOfWork.Query<Invoice>()
                    .Where(i => i.DueDate < DateTime.Now
                                && i.PaidAmount < i.TotalAmount
                                && i.Status != "Ödendi"
                                && i.Status != "İptal")
                    .ToListAsync();

                _logger.LogInformation($"{overdueInvoices.Count} adet gecikmiş fatura bulundu.");

                foreach (var invoice in overdueInvoices)
                {
                    invoice.Status = "Gecikmiş";
                    invoice.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.Update(invoice);
                }

                await unitOfWork.CompleteAsync();

                // Action Log'ları kaydet
                foreach (var invoice in overdueInvoices)
                {
                    await logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "UPDATE",
                        EntityType = "Invoice",
                        EntityId = invoice.Id,
                        AdditionalInfo = $"Fatura otomatik olarak Gecikmiş olarak işaretlendi. Son ödeme tarihi: {invoice.DueDate}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gecikmiş faturalar güncellenirken hata oluştu");
            }
        }
    }
}
