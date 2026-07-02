using AutoMapper;
using ClosedXML.Excel;
using Crm.Application.DTOs.Dashboard;
using Crm.Application.DTOs.Reports;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;


namespace Crm.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReportService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<DashboardDto> GetDashboardDataAsync()
        {
            // 🔥 TÜM VERİLERİ ÇEK - IsDeleted NULL olanları da al
            var tickets = await _unitOfWork.Query<Ticket>()
                .Where(t => t.IsDeleted == false || t.IsDeleted == null)  // ← BURASI ÖNEMLİ
                .Include(t => t.Customer)
                .Include(t => t.AssignedToPersonel)
                .Include(t => t.CreatedByPersonel)
                .ToListAsync();

            var leads = await _unitOfWork.Query<Lead>()
                .Where(l => l.IsDeleted == false || l.IsDeleted == null)
                .Include(l => l.AssignedToPersonel)
                .Include(l => l.Campaign)
                .ToListAsync();

            var invoices = await _unitOfWork.Query<Invoice>()
                .Where(i => i.IsDeleted == false || i.IsDeleted == null)
                .Include(i => i.Customer)
                .Include(i => i.CreatedByPersonel)
                .ToListAsync();

            var opportunities = await _unitOfWork.Query<Opportunity>()
                .Where(o => o.IsDeleted == false || o.IsDeleted == null)
                .Include(o => o.Customer)
                .Include(o => o.AssignedToPersonel)
                .ToListAsync();

            var customers = await _unitOfWork.Query<Customer>()
                .Where(c => c.IsDeleted == false || c.IsDeleted == null)
                .ToListAsync();

            var personels = await _unitOfWork.Query<Personel>()
                .Where(p => p.IsDeleted == false || p.IsDeleted == null)
                .ToListAsync();

            var orders = await _unitOfWork.Query<Order>()
                .Where(o => o.IsDeleted == false || o.IsDeleted == null)
                .ToListAsync();

            // KPI hesapla
            var kpi = new DashboardKpiDto
            {
                TotalTickets = tickets.Count,
                OpenTickets = tickets.Count(t =>
                    t.Status == "Açık" || t.Status == "Open" || t.Status == "OPEN" || t.Status == "open"),
                InProgressTickets = tickets.Count(t =>
                    t.Status == "İşlemde" || t.Status == "InProgress" || t.Status == "In Progress" || t.Status == "inprogress" || t.Status == "in progress"),
                ResolvedTickets = tickets.Count(t =>
                    t.Status == "Çözüldü" || t.Status == "Resolved" || t.Status == "resolved"),
                ClosedTickets = tickets.Count(t =>
                    t.Status == "Kapandı" || t.Status == "Closed" || t.Status == "closed"),

                TotalLeads = leads.Count,
                NewLeads = leads.Count(l => l.Status == "Yeni" || l.Status == "New"),
                QualifiedLeads = leads.Count(l => l.Status == "İletişime Geçildi" || l.Status == "Contacted" || l.Status == "Teklif Sunuldu" || l.Status == "Proposal Sent" || l.Status == "Qualified"),
                ConvertedLeads = leads.Count(l => l.Status == "Müşteri Oldu" || l.Status == "Converted" || l.Status == "Customer"),
                LostLeads = leads.Count(l => l.Status == "Kaybedildi" || l.Status == "Lost"),
                ConversionRate = leads.Count > 0 ? Math.Round((decimal)leads.Count(l => l.Status == "Müşteri Oldu" || l.Status == "Converted" || l.Status == "Customer") / leads.Count * 100, 2) : 0,

                TotalRevenue = invoices.Sum(i => i.TotalAmount),
                TotalPaid = invoices.Sum(i => i.PaidAmount),
                TotalReceivable = invoices.Sum(i => i.TotalAmount - i.PaidAmount),
                AverageOrderValue = orders.Count > 0 ? Math.Round(orders.Average(o => o.TotalAmount), 2) : 0,
                TotalOrders = orders.Count,

                TotalOpportunities = opportunities.Count,
                TotalOpportunityValue = opportunities.Sum(o => o.Amount),
                WonOpportunities = opportunities.Count(o => o.Stage == "Kapandı-Kazandı" || o.Stage == "Closed-Won" || o.Stage == "Won"),
                LostOpportunities = opportunities.Count(o => o.Stage == "Kapandı-Kaybetti" || o.Stage == "Closed-Lost" || o.Stage == "Lost"),
                WinRate = opportunities.Count > 0 ? Math.Round((decimal)opportunities.Count(o => o.Stage == "Kapandı-Kazandı" || o.Stage == "Closed-Won" || o.Stage == "Won") / opportunities.Count * 100, 2) : 0,

                TotalPersonnel = personels.Count,
                ActivePersonnel = personels.Count(p => p.IsActive),
                TotalCustomers = customers.Count,
                ActiveCustomers = customers.Count(c => c.IsActive)
            };

            // Grafikler
            // Grafikler
            var charts = new List<DashboardChartDto>();

            if (tickets.Count > 0)
            {
                // 1. Ticket Durum Dağılımı (Pie) - AYNI KALABİLİR
                charts.Add(new DashboardChartDto
                {
                    Title = "Ticket Durum Dağılımı",
                    Type = "Pie",
                    Data = tickets.GroupBy(t => t.Status ?? "Belirsiz")
                        .Select(g => new ChartDataPoint
                        {
                            Label = g.Key,
                            Value = g.Count(),
                            Percentage = Math.Round((decimal)g.Count() / tickets.Count * 100, 2)
                        })
                        .ToList()
                });

           
             
                var ticketList = tickets
                    .Select((t, index) => new ChartDataPoint
                    {
                        Label = $"Ticket #{t.Id}",
                        Value = 1
                    })
                    .ToList();

                charts.Add(new DashboardChartDto
                {
                    Title = "Ticket Listesi (Her Ticket 1 Değerinde)",
                    Type = "Bar",  // Bar grafik daha net gösterir
                    Data = ticketList
                });

           
                var weeklyTickets = tickets
                    .GroupBy(t => t.CreatedAt.ToString("dd.MM.yyyy"))
                    .Select(g => new ChartDataPoint
                    {
                        Label = g.Key,
                        Value = g.Count()
                    })
                    .OrderBy(x => x.Label)
                    .ToList();

                charts.Add(new DashboardChartDto
                {
                    Title = "Günlük Ticket Dağılımı",
                    Type = "Line",
                    Data = weeklyTickets
                });
            }

            if (leads.Count > 0)
            {
                charts.Add(new DashboardChartDto
                {
                    Title = "Lead Durum Dağılımı",
                    Type = "Donut",
                    Data = leads.GroupBy(l => l.Status ?? "Belirsiz")
                        .Select(g => new ChartDataPoint
                        {
                            Label = g.Key,
                            Value = g.Count(),
                            Percentage = Math.Round((decimal)g.Count() / leads.Count * 100, 2)
                        })
                        .ToList()
                });
            }

            if (personels.Count > 0 && tickets.Count > 0)
            {
                var personnelPerformance = personels
                    .Select(p => new ChartDataPoint
                    {
                        Label = $"{p.FirstName} {p.LastName}",
                        Value = tickets.Count(t =>
                            t.AssignedToPersonelId == p.Id &&
                            (t.Status == "Çözüldü" || t.Status == "Resolved" || t.Status == "Kapandı" || t.Status == "Closed"))
                    })
                    .Where(x => x.Value > 0)
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .ToList();

                if (personnelPerformance.Any())
                {
                    charts.Add(new DashboardChartDto
                    {
                        Title = "Personel Performansı (Çözülen Ticket)",
                        Type = "Bar",
                        Data = personnelPerformance
                    });
                }
            }

            if (invoices.Count > 0)
            {
                var monthlyRevenue = invoices
                    .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                    .Select(g => new ChartDataPoint
                    {
                        Label = $"{g.Key.Month}/{g.Key.Year}",
                        Value = g.Sum(i => i.TotalAmount)
                    })
                    .OrderBy(x => x.Label)
                    .ToList();

                charts.Add(new DashboardChartDto
                {
                    Title = "Aylık Gelir Trendi",
                    Type = "Line",
                    Data = monthlyRevenue
                });
            }

            // Aktiviteler
            var activities = new List<DashboardActivityDto>();

            var recentTickets = tickets
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new DashboardActivityDto
                {
                    Id = t.Id,
                    EntityType = "Ticket",
                    Action = "Oluşturuldu",
                    Title = t.Subject,
                    UserName = t.CreatedByPersonel != null ? $"{t.CreatedByPersonel.FirstName} {t.CreatedByPersonel.LastName}" : "Sistem",
                    CreatedAt = t.CreatedAt,
                    TimeAgo = GetTimeAgo(t.CreatedAt)
                })
                .ToList();
            activities.AddRange(recentTickets);

            var recentLeads = leads
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .Select(l => new DashboardActivityDto
                {
                    Id = l.Id,
                    EntityType = "Lead",
                    Action = "Eklendi",
                    Title = l.CompanyName,
                    UserName = l.AssignedToPersonel != null ? $"{l.AssignedToPersonel.FirstName} {l.AssignedToPersonel.LastName}" : "Atanmadı",
                    CreatedAt = l.CreatedAt,
                    TimeAgo = GetTimeAgo(l.CreatedAt)
                })
                .ToList();
            activities.AddRange(recentLeads);

            var recentInvoices = invoices
                .OrderByDescending(i => i.CreatedAt)
                .Take(10)
                .Select(i => new DashboardActivityDto
                {
                    Id = i.Id,
                    EntityType = "Invoice",
                    Action = "Oluşturuldu",
                    Title = $"{i.InvoiceNumber} - {i.Customer?.FirstName} {i.Customer?.LastName}",
                    UserName = i.Customer?.FirstName ?? "Bilinmiyor",
                    CreatedAt = i.CreatedAt,
                    TimeAgo = GetTimeAgo(i.CreatedAt)
                })
                .ToList();
            activities.AddRange(recentInvoices);

            var recentOpportunities = opportunities
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new DashboardActivityDto
                {
                    Id = o.Id,
                    EntityType = "Opportunity",
                    Action = "Eklendi",
                    Title = o.Name,
                    UserName = o.AssignedToPersonel != null ? $"{o.AssignedToPersonel.FirstName} {o.AssignedToPersonel.LastName}" : "Atanmadı",
                    CreatedAt = o.CreatedAt,
                    TimeAgo = GetTimeAgo(o.CreatedAt)
                })
                .ToList();
            activities.AddRange(recentOpportunities);

            var sortedActivities = activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(30)
                .ToList();

            return new DashboardDto
            {
                Kpi = kpi,
                Charts = charts,
                RecentActivities = sortedActivities,
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<ReportDataDto> GetReportDataAsync(ReportFilterDto filter)
        {
            // 🔥 LOG
            Console.WriteLine($"📥 Gelen Type: '{filter.Type}'");

            // 🔥 Type zaten enum olduğu için direkt kullan
            var reportType = filter.Type;

            switch (reportType)
            {
                case ReportType.TicketStatusReport:
                    return await GetTicketStatusReportAsync(filter);
                case ReportType.TicketPerformanceReport:
                    return await GetTicketPerformanceReportAsync(filter);
                case ReportType.TicketCategoryReport:
                    return await GetTicketCategoryReportAsync(filter);
                case ReportType.TicketPriorityReport:
                    return await GetTicketPriorityReportAsync(filter);
                case ReportType.LeadStatusReport:
                    return await GetLeadStatusReportAsync(filter);
                case ReportType.LeadConversionReport:
                    return await GetLeadConversionReportAsync(filter);
                case ReportType.LeadSourceReport:
                    return await GetLeadSourceReportAsync(filter);
                case ReportType.OpportunityReport:
                    return await GetOpportunityReportAsync(filter);
                case ReportType.RevenueReport:
                    return await GetRevenueReportAsync(filter);
                case ReportType.CustomerReport:
                    return await GetCustomerReportAsync(filter);
                case ReportType.PersonnelPerformanceReport:
                    return await GetPersonnelPerformanceReportAsync(filter);
                case ReportType.FinancialReport:
                    return await GetFinancialReportAsync(filter);
                case ReportType.CampaignReport:
                    return await GetCampaignReportAsync(filter);
                case ReportType.DashboardKPI:
                    return await GetDashboardKPIReportAsync(filter);
                default:
                    throw new ArgumentException($"Desteklenmeyen rapor tipi: {filter.Type}");
            }
        }


        // ==================== DASHBOARD KPI RAPORU ====================

        private async Task<ReportDataDto> GetDashboardKPIReportAsync(ReportFilterDto filter)
        {
            // Dashboard verilerini al
            var dashboardData = await GetDashboardDataAsync();

            // Rapor formatına dönüştür
            var items = new List<Dictionary<string, object>>
    {
        new() {
            ["Toplam Müşteri"] = dashboardData.Kpi.TotalCustomers,
            ["Aktif Müşteri"] = dashboardData.Kpi.ActiveCustomers,
            ["Toplam Personel"] = dashboardData.Kpi.TotalPersonnel,
            ["Aktif Personel"] = dashboardData.Kpi.ActivePersonnel,
            ["Toplam Ticket"] = dashboardData.Kpi.TotalTickets,
            ["Açık Ticket"] = dashboardData.Kpi.OpenTickets,
            ["İşlemde Ticket"] = dashboardData.Kpi.InProgressTickets,
            ["Çözülen Ticket"] = dashboardData.Kpi.ResolvedTickets,
            ["Kapalı Ticket"] = dashboardData.Kpi.ClosedTickets,
            ["Toplam Lead"] = dashboardData.Kpi.TotalLeads,
            ["Yeni Lead"] = dashboardData.Kpi.NewLeads,
            ["Müşteri Olan Lead"] = dashboardData.Kpi.ConvertedLeads,
            ["Dönüşüm Oranı"] = dashboardData.Kpi.ConversionRate,
            ["Toplam Fırsat"] = dashboardData.Kpi.TotalOpportunities,
            ["Kazanılan Fırsat"] = dashboardData.Kpi.WonOpportunities,
            ["Kaybedilen Fırsat"] = dashboardData.Kpi.LostOpportunities,
            ["Kazanma Oranı"] = dashboardData.Kpi.WinRate,
            ["Toplam Sipariş"] = dashboardData.Kpi.TotalOrders,
            ["Toplam Gelir"] = dashboardData.Kpi.TotalRevenue,
            ["Ödenen Tutar"] = dashboardData.Kpi.TotalPaid,
            ["Alacak"] = dashboardData.Kpi.TotalReceivable,
            ["Ortalama Sipariş"] = dashboardData.Kpi.AverageOrderValue
        }
    };

            return new ReportDataDto
            {
                Title = "Dashboard KPI Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Items = items,
                TotalCount = items.Count,
            };
        }


        public async Task<byte[]> GenerateReportAsync(ReportFilterDto filter)
        {
            var reportData = await GetReportDataAsync(filter);
            var json = System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            return System.Text.Encoding.UTF8.GetBytes(json);
        }


        // Export excell
        public async Task<byte[]> ExportToExcelAsync(ReportFilterDto filter)
        {
            var reportData = await GetReportDataAsync(filter);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Rapor");

                // ========== 1. BAŞLIK ==========
                worksheet.Cell(1, 1).Value = "📊 CRM RAPORU";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 18;
                worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(0, 51, 102);
                worksheet.Range(1, 1, 1, 6).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(2, 1).Value = $"Oluşturulma Tarihi: {DateTime.Now:dd MMMM yyyy HH:mm}";
                worksheet.Cell(2, 1).Style.Font.FontSize = 10;
                worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
                worksheet.Range(2, 1, 2, 6).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Row(3).Height = 15;

                // ========== 2. ÖZET KARTLARI ==========
                if (reportData.Summary != null)
                {
                    worksheet.Cell(4, 1).Value = "📈 ÖZET";
                    worksheet.Cell(4, 1).Style.Font.Bold = true;
                    worksheet.Cell(4, 1).Style.Font.FontSize = 14;
                    worksheet.Cell(4, 1).Style.Font.FontColor = XLColor.FromArgb(0, 51, 102);
                    worksheet.Range(4, 1, 4, 6).Merge();

                    int kpiRow = 5;
                    if (reportData.Summary.TotalRecords > 0)
                    {
                        worksheet.Cell(kpiRow, 1).Value = "Toplam Kayıt";
                        worksheet.Cell(kpiRow, 1).Style.Font.Bold = true;
                        worksheet.Cell(kpiRow, 1).Style.Font.FontSize = 12;
                        worksheet.Cell(kpiRow, 1).Style.Font.FontColor = XLColor.FromArgb(64, 64, 64);
                        worksheet.Cell(kpiRow, 2).Value = reportData.Summary.TotalRecords;
                        worksheet.Cell(kpiRow, 2).Style.Font.FontSize = 14;
                        worksheet.Cell(kpiRow, 2).Style.Font.FontColor = XLColor.FromArgb(0, 128, 0);
                        kpiRow++;
                    }

                    if (reportData.Summary.TotalAmount.HasValue && reportData.Summary.TotalAmount > 0)
                    {
                        worksheet.Cell(kpiRow, 1).Value = "Toplam Tutar";
                        worksheet.Cell(kpiRow, 1).Style.Font.Bold = true;
                        worksheet.Cell(kpiRow, 1).Style.Font.FontSize = 12;
                        worksheet.Cell(kpiRow, 1).Style.Font.FontColor = XLColor.FromArgb(64, 64, 64);
                        worksheet.Cell(kpiRow, 2).Value = reportData.Summary.TotalAmount.Value;
                        worksheet.Cell(kpiRow, 2).Style.NumberFormat.Format = "#,##0.00 ₺";
                        worksheet.Cell(kpiRow, 2).Style.Font.FontSize = 14;
                        worksheet.Cell(kpiRow, 2).Style.Font.FontColor = XLColor.FromArgb(0, 128, 0);
                        kpiRow++;
                    }
                    worksheet.Row(kpiRow).Height = 15;
                }

                // ========== 3. TABLO ==========
                int startRow = 8;
                worksheet.Cell(startRow, 1).Value = "📋 DETAYLI VERİLER";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 1).Style.Font.FontSize = 14;
                worksheet.Cell(startRow, 1).Style.Font.FontColor = XLColor.FromArgb(0, 51, 102);
                worksheet.Range(startRow, 1, startRow, 6).Merge();
                startRow += 2;

                // 🔥 HEADER GÜNCELLEMESİ: Dinamik ve güvenli
                var headers = reportData.Items?.FirstOrDefault()?.Keys.ToList() ?? new List<string>();
                if (headers.Count == 0)
                {
                    // Eğer hiç veri yoksa kullanıcıya bilgi ver
                    worksheet.Cell(startRow, 1).Value = "Raporlanacak veri bulunamadı.";
                    worksheet.Cell(startRow, 1).Style.Font.FontColor = XLColor.Red;
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return stream.ToArray();
                    }
                }

                int headerRowIndex = startRow;
                var headerRow = worksheet.Row(headerRowIndex);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Font.FontSize = 11;
                headerRow.Style.Font.FontColor = XLColor.Black;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 225, 230);
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRow.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Header'ları yaz
                for (int i = 0; i < headers.Count; i++)
                {
                    var cell = worksheet.Cell(headerRowIndex, i + 1);
                    cell.Value = headers[i]?.Trim() ?? $"Sütun {i + 1}";
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontSize = 11;
                    cell.Style.Font.FontColor = XLColor.Black;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 225, 230);
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                    cell.Style.Border.BottomBorderColor = XLColor.Black;
                    cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                }

                startRow++;
                int dataStartRow = startRow;

                // 🔥 VERİ YAZMA: Performanslı ve güvenli
                foreach (var item in reportData.Items)
                {
                    int col = 1;
                    foreach (var header in headers)
                    {
                        var cell = worksheet.Cell(startRow, col);
                        if (item.TryGetValue(header, out var value))
                        {
                            if (value is decimal dec)
                            {
                                cell.Value = dec;
                                cell.Style.NumberFormat.Format = "#,##0.00";
                            }
                            else if (value is int integer)
                            {
                                cell.Value = integer;
                                cell.Style.NumberFormat.Format = "#,##0";
                            }
                            else if (value is DateTime date)
                            {
                                cell.Value = date;
                                cell.Style.NumberFormat.Format = "dd.MM.yyyy HH:mm";
                            }
                            else
                            {
                                cell.Value = value?.ToString() ?? "";
                            }
                        }
                        else
                        {
                            cell.Value = "";
                        }

                        cell.Style.Font.FontColor = XLColor.Black;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                        col++;
                    }
                    startRow++;
                }

                // Alternatif satır renklendirme
                for (int row = dataStartRow; row < startRow; row++)
                {
                    if (row % 2 == 0)
                    {
                        worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 248, 255);
                    }
                }

                // 🔥 SADECE AdjustToContents KULLAN
                worksheet.Columns().AdjustToContents();

                // Footer
                var footerRow = startRow + 1;
                worksheet.Cell(footerRow, 1).Value = $"Rapor {DateTime.Now:yyyy-MM-dd HH:mm:ss} tarihinde oluşturuldu. | CRM Sistemi v1.0";
                worksheet.Cell(footerRow, 1).Style.Font.FontSize = 9;
                worksheet.Cell(footerRow, 1).Style.Font.FontColor = XLColor.Gray;
                worksheet.Range(footerRow, 1, footerRow, 6).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        // Export Pdf
        public async Task<byte[]> ExportToPdfAsync(ReportFilterDto filter)
        {
            var reportData = await GetReportDataAsync(filter);

            QuestPDF.Settings.License = LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    // 🔥 YATAY (LANDSCAPE)
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    // ========== HEADER ==========
                    page.Header()
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Column(col =>
                                {
                                    col.Item()
                                        .Text("CRM Raporu")
                                        .Bold()
                                        .FontSize(18)
                                        .FontColor(Colors.Blue.Darken2);

                                    col.Item()
                                        .Text($"Oluşturulma Tarihi: {DateTime.Now:dd MMMM yyyy HH:mm}")
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Medium);
                                });
                        });

                    // ========== CONTENT ==========
                    page.Content()
                        .PaddingVertical(0.5f, Unit.Centimetre)
                        .Column(col =>
                        {
                            col.Item()
                                .Table(table =>
                                {
                                    var headers = reportData.Items.FirstOrDefault()?.Keys.ToList() ?? new List<string>();

                                    table.ColumnsDefinition(columns =>
                                    {
                                        foreach (var _ in headers)
                                        {
                                            columns.RelativeColumn();
                                        }
                                    });

                                    // Başlıklar
                                    table.Header(header =>
                                    {
                                        foreach (var headerText in headers)
                                        {
                                            header.Cell()
                                                .Background(Colors.Blue.Darken2)
                                                .Padding(4f)
                                                .Text(headerText)
                                                .Bold()
                                                .FontColor(Colors.White)
                                                .FontSize(9);
                                        }
                                    });

                                    // Veriler
                                    foreach (var item in reportData.Items)
                                    {
                                        foreach (var header in headers)
                                        {
                                            var value = item.ContainsKey(header) ? item[header] : null;
                                            table.Cell()
                                                .Padding(3f)
                                                .Text(value?.ToString() ?? "")
                                                .FontSize(8);
                                        }
                                    }
                                });

                            col.Item()
                                .PaddingTop(5f)
                                .Text($"Rapor {DateTime.Now:yyyy-MM-dd HH:mm:ss} tarihinde oluşturuldu.")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                        });

                 
                    // ========== FOOTER ==========
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Sayfa ").FontSize(8).FontColor(Colors.Grey.Medium);
                            text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                            text.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                            text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                });
            });

            using (var stream = new MemoryStream())
            {
                document.GeneratePdf(stream);
                return stream.ToArray();
            }
        }

        // ==================== Özel Rapor Metodları ====================

        private async Task<ReportDataDto> GetTicketStatusReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Ticket>()
                .Where(t => !t.IsDeleted)
                .Include(t => t.Customer)
                .Include(t => t.AssignedToPersonel)
                .Include(t => t.CreatedByPersonel)
                .AsQueryable();

            query = ApplyTicketFilters(query, filter);

            var tickets = await query.ToListAsync();

            // 🔥🔥🔥 LOG EKLE - Bakalım kaç ticket geliyor
            Console.WriteLine($"📊 TICKET SAYISI: {tickets.Count}");

            var items = tickets.Select(t => new Dictionary<string, object>
            {
                ["Ticket No"] = t.TicketNumber,
                ["Konu"] = t.Subject,
                ["Müşteri"] = t.Customer != null ? $"{t.Customer.FirstName} {t.Customer.LastName}" : "-",
                ["Durum"] = t.Status ?? "Belirsiz",
                ["Öncelik"] = t.Priority ?? "Belirsiz",
                ["Kategori"] = t.Category ?? "Belirsiz",
                ["Atanan"] = t.AssignedToPersonel != null ? $"{t.AssignedToPersonel.FirstName} {t.AssignedToPersonel.LastName}" : "Atanmadı",
                ["Oluşturan"] = t.CreatedByPersonel != null ? $"{t.CreatedByPersonel.FirstName} {t.CreatedByPersonel.LastName}" : "Sistem",
                ["Oluşturma"] = t.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                ["Çözülme"] = t.ResolvedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-"
            }).ToList();

           
            Console.WriteLine($" ITEMS SAYISI: {items.Count}");

            var summary = new ReportSummaryDto
            {
                TotalRecords = items.Count,
                StatusDistribution = tickets.GroupBy(t => t.Status ?? "Belirsiz")
                    .ToDictionary(g => g.Key, g => g.Count()),
                CategoryDistribution = tickets.GroupBy(t => t.Category ?? "Belirsiz")
                    .ToDictionary(g => g.Key, g => g.Count()),
                PersonnelDistribution = tickets
                    .Where(t => t.AssignedToPersonel != null)
                    .GroupBy(t => $"{t.AssignedToPersonel.FirstName} {t.AssignedToPersonel.LastName}")
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return new ReportDataDto
            {
                Title = "Ticket Durum Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = items,  // ← eğer items boşsa, frontend'de hata alırsın
                TotalCount = items.Count
            };
        }

        private async Task<ReportDataDto> GetTicketPerformanceReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Ticket>()
                .Where(t => !t.IsDeleted)
                .Include(t => t.AssignedToPersonel)
                .AsQueryable();

            query = ApplyTicketFilters(query, filter);

            var tickets = await query.ToListAsync();

            var personelPerformance = tickets
                .Where(t => t.AssignedToPersonel != null)
                .GroupBy(t => t.AssignedToPersonelId)
                .Select(g => new
                {
                    Personel = tickets.First(t => t.AssignedToPersonelId == g.Key).AssignedToPersonel,
                    TotalAssigned = g.Count(),
                    Resolved = g.Count(t => t.Status == "Çözüldü"),
                    Closed = g.Count(t => t.Status == "Kapandı"),
                    AvgResolutionTime = g.Where(t => t.ResolvedAt.HasValue)
                        .Select(t => (t.ResolvedAt.Value - t.CreatedAt).TotalHours)
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .ToList();

            var items = personelPerformance.Select(p => new Dictionary<string, object>
            {
                ["Personel"] = p.Personel != null ? $"{p.Personel.FirstName} {p.Personel.LastName}" : "-",
                ["Atanan Ticket"] = p.TotalAssigned,
                ["Çözülen"] = p.Resolved,
                ["Kapatılan"] = p.Closed,
                ["Çözüm Oranı"] = p.TotalAssigned > 0 ? Math.Round((decimal)p.Resolved / p.TotalAssigned * 100, 2) : 0,
                ["Ort. Çözüm Süresi (saat)"] = Math.Round(p.AvgResolutionTime, 2)
            }).ToList();

            var summary = new ReportSummaryDto
            {
                TotalRecords = items.Count,
                Extra = new Dictionary<string, object>
                {
                    ["Toplam Ticket"] = tickets.Count,
                    ["Toplam Çözülen"] = tickets.Count(t => t.Status == "Çözüldü"),
                    ["Genel Çözüm Oranı"] = tickets.Count > 0
                        ? Math.Round((decimal)tickets.Count(t => t.Status == "Çözüldü") / tickets.Count * 100, 2)
                        : 0
                }
            };

            return new ReportDataDto
            {
                Title = "Personel Performans Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = items,
                TotalCount = items.Count
            };
        }

        private async Task<ReportDataDto> GetLeadStatusReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Lead>()
                .Where(l => !l.IsDeleted)
                .Include(l => l.AssignedToPersonel)
                .Include(l => l.Campaign)
                .Include(l => l.ConvertedToCustomer)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.CreatedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(l => l.CreatedAt <= filter.EndDate.Value);
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(l => l.Status == filter.Status);
            if (!string.IsNullOrEmpty(filter.Source))
                query = query.Where(l => l.Source == filter.Source);
            if (filter.PersonelId.HasValue)
                query = query.Where(l => l.AssignedToPersonelId == filter.PersonelId.Value);
            if (filter.CampaignId.HasValue)
                query = query.Where(l => l.CampaignId == filter.CampaignId.Value);

            var leads = await query.ToListAsync();

            var items = leads.Select(l => new Dictionary<string, object>
            {
                ["Şirket"] = l.CompanyName,
                ["İletişim"] = l.ContactName,
                ["Email"] = l.Email,
                ["Telefon"] = l.Phone,
                ["Durum"] = l.Status ?? "Belirsiz",
                ["Kaynak"] = l.Source ?? "Belirsiz",
                ["Atanan"] = l.AssignedToPersonel != null ? $"{l.AssignedToPersonel.FirstName} {l.AssignedToPersonel.LastName}" : "Atanmadı",
                ["Potansiyel Gelir"] = l.PotentialRevenue?.ToString("N2") ?? "-",
                ["Kampanya"] = l.Campaign?.Name ?? "-",
                ["Müşteri Oldu"] = l.ConvertedToCustomer != null ? "Evet" : "Hayır",
                ["Oluşturma"] = l.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                ["Takip"] = l.NextFollowUpDate?.ToString("dd.MM.yyyy") ?? "-"
            }).ToList();

            var summary = new ReportSummaryDto
            {
                TotalRecords = items.Count,
                StatusDistribution = leads.GroupBy(l => l.Status ?? "Belirsiz")
                    .ToDictionary(g => g.Key, g => g.Count()),
                PersonnelDistribution = leads
                    .Where(l => l.AssignedToPersonel != null)
                    .GroupBy(l => $"{l.AssignedToPersonel.FirstName} {l.AssignedToPersonel.LastName}")
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return new ReportDataDto
            {
                Title = "Lead Durum Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = items,
                TotalCount = items.Count
            };
        }

        private async Task<ReportDataDto> GetLeadConversionReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Lead>()
                .Where(l => !l.IsDeleted)
                .Include(l => l.AssignedToPersonel)
                .Include(l => l.ConvertedToCustomer)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.CreatedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(l => l.CreatedAt <= filter.EndDate.Value);

            var leads = await query.ToListAsync();

            var converted = leads.Count(l => l.ConvertedToCustomer != null);
            var total = leads.Count;

            var items = new List<Dictionary<string, object>>
            {
                new() {
                    ["Toplam Lead"] = total,
                    ["Dönüşen"] = converted,
                    ["Dönüşüm Oranı"] = total > 0 ? Math.Round((decimal)converted / total * 100, 2) : 0
                }
            };

            // Kaynak bazında dönüşüm
            var sourceConversion = leads
                .GroupBy(l => l.Source ?? "Belirsiz")
                .Select(g => new
                {
                    Source = g.Key,
                    Total = g.Count(),
                    Converted = g.Count(l => l.ConvertedToCustomer != null)
                })
                .ToList();

            foreach (var item in sourceConversion)
            {
                items.Add(new Dictionary<string, object>
                {
                    ["Kaynak"] = item.Source,
                    ["Toplam"] = item.Total,
                    ["Dönüşen"] = item.Converted,
                    ["Dönüşüm Oranı"] = item.Total > 0 ? Math.Round((decimal)item.Converted / item.Total * 100, 2) : 0
                });
            }

            var summary = new ReportSummaryDto
            {
                TotalRecords = items.Count,
                Extra = new Dictionary<string, object>
                {
                    ["Toplam Lead"] = total,
                    ["Dönüşen"] = converted,
                    ["Dönüşüm Oranı"] = total > 0 ? Math.Round((decimal)converted / total * 100, 2) : 0
                }
            };

            return new ReportDataDto
            {
                Title = "Lead Dönüşüm Raporu",
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = items,
                TotalCount = items.Count
            };
        }

        private async Task<ReportDataDto> GetRevenueReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Invoice>()
                .Where(i => !i.IsDeleted)
                .Include(i => i.Customer)
                .Include(i => i.CreatedByPersonel)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(i => i.InvoiceDate >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(i => i.InvoiceDate <= filter.EndDate.Value);
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(i => i.Status == filter.Status);
            if (filter.CustomerId.HasValue)
                query = query.Where(i => i.CustomerId == filter.CustomerId.Value);

            var invoices = await query.ToListAsync();

            var items = invoices.Select(i => new Dictionary<string, object>
            {
                ["Fatura No"] = i.InvoiceNumber,
                ["Müşteri"] = i.Customer != null ? $"{i.Customer.FirstName} {i.Customer.LastName}" : "-",
                ["Tarih"] = i.InvoiceDate.ToString("dd.MM.yyyy"),
                ["Vade"] = i.DueDate.ToString("dd.MM.yyyy"),
                ["Ara Toplam"] = i.SubTotal.ToString("N2"),
                ["KDV"] = i.TaxAmount.ToString("N2"),
                ["Toplam"] = i.TotalAmount.ToString("N2"),
                ["Ödenen"] = i.PaidAmount.ToString("N2"),
                ["Kalan"] = (i.TotalAmount - i.PaidAmount).ToString("N2"),
                ["Durum"] = i.Status ?? "Belirsiz",
                ["Oluşturan"] = i.CreatedByPersonel != null ? $"{i.CreatedByPersonel.FirstName} {i.CreatedByPersonel.LastName}" : "Sistem"
            }).ToList();

            var summary = new ReportSummaryDto
            {
                TotalRecords = items.Count,
                TotalAmount = invoices.Sum(i => i.TotalAmount),
                AverageAmount = invoices.Count > 0 ? invoices.Average(i => i.TotalAmount) : 0,
                MinAmount = invoices.Count > 0 ? invoices.Min(i => i.TotalAmount) : 0,
                MaxAmount = invoices.Count > 0 ? invoices.Max(i => i.TotalAmount) : 0,
                StatusDistribution = invoices.GroupBy(i => i.Status ?? "Belirsiz")
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return new ReportDataDto
            {
                Title = "Gelir Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = items,
                TotalCount = items.Count
            };
        }

        private async Task<ReportDataDto> GetOpportunityReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Opportunity>()
                .Where(o => !o.IsDeleted)
                .Include(o => o.Customer)
                .Include(o => o.AssignedToPersonel)
                .Include(o => o.CreatedByPersonel)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(o => o.CreatedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(o => o.CreatedAt <= filter.EndDate.Value);
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(o => o.Status == filter.Status);
            if (!string.IsNullOrEmpty(filter.Stage))
                query = query.Where(o => o.Stage == filter.Stage);
            if (filter.PersonelId.HasValue)
                query = query.Where(o => o.AssignedToPersonelId == filter.PersonelId.Value);
            if (filter.CustomerId.HasValue)
                query = query.Where(o => o.CustomerId == filter.CustomerId.Value);

            var opportunities = await query.ToListAsync();

            var items = opportunities.Select(o => new Dictionary<string, object>
            {
                ["Fırsat"] = o.Name,
                ["Müşteri"] = o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}" : "-",
                ["Tutar"] = o.Amount.ToString("N2"),
                ["Aşama"] = o.Stage ?? "Belirsiz",
                ["Durum"] = o.Status ?? "Belirsiz",
                ["Atanan"] = o.AssignedToPersonel != null ? $"{o.AssignedToPersonel.FirstName} {o.AssignedToPersonel.LastName}" : "Atanmadı",
                ["Beklenen Kapanış"] = o.ExpectedCloseDate?.ToString("dd.MM.yyyy") ?? "-",
                ["Gerçek Kapanış"] = o.ActualCloseDate?.ToString("dd.MM.yyyy") ?? "-",
                ["Kayıp Nedeni"] = o.LostReason ?? "-",
                ["Oluşturma"] = o.CreatedAt.ToString("dd.MM.yyyy HH:mm")
            }).ToList();

            var summary = new ReportSummaryDto
            {
                TotalRecords = items.Count,
                TotalAmount = opportunities.Sum(o => o.Amount),
                AverageAmount = opportunities.Count > 0 ? opportunities.Average(o => o.Amount) : 0,
                PersonnelDistribution = opportunities
                    .Where(o => o.AssignedToPersonel != null)
                    .GroupBy(o => $"{o.AssignedToPersonel.FirstName} {o.AssignedToPersonel.LastName}")
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return new ReportDataDto
            {
                Title = "Fırsat Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = items,
                TotalCount = items.Count
            };
        }

        private async Task<ReportDataDto> GetPersonnelPerformanceReportAsync(ReportFilterDto filter)
        {
            var personels = await _unitOfWork.Query<Personel>()
                .Where(p => !p.IsDeleted && p.IsActive)
                .Include(p => p.AssignedTickets)
                .Include(p => p.AssignedLeads)
                .Include(p => p.AssignedTasks)
                .ToListAsync();

            var items = personels.Select(p => new Dictionary<string, object>
            {
                ["Personel"] = $"{p.FirstName} {p.LastName}",
                ["Departman"] = p.Department?.Name ?? "-",
                ["Unvan"] = p.Position?.Name ?? "-",
                ["Atanan Ticket"] = p.AssignedTickets.Count,
                ["Çözülen Ticket"] = p.AssignedTickets.Count(t => t.Status == "Çözüldü"),
                ["Kapatılan Ticket"] = p.AssignedTickets.Count(t => t.Status == "Kapandı"),
                ["Ticket Çözüm Oranı"] = p.AssignedTickets.Count > 0
                    ? Math.Round((decimal)p.AssignedTickets.Count(t => t.Status == "Çözüldü") / p.AssignedTickets.Count * 100, 2)
                    : 0,
                ["Atanan Lead"] = p.AssignedLeads.Count,
                ["Dönüşen Lead"] = p.AssignedLeads.Count(l => l.ConvertedToCustomer != null),
                ["Lead Dönüşüm Oranı"] = p.AssignedLeads.Count > 0
                    ? Math.Round((decimal)p.AssignedLeads.Count(l => l.ConvertedToCustomer != null) / p.AssignedLeads.Count * 100, 2)
                    : 0,
                ["Atanan Görev"] = p.AssignedTasks.Count,
                ["Tamamlanan Görev"] = p.AssignedTasks.Count(t => t.Status == "Tamamlandı")
            }).ToList();

            var summary = new ReportSummaryDto
            {
                TotalRecords = items.Count
            };

            return new ReportDataDto
            {
                Title = "Personel Performans Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = items,
                TotalCount = items.Count
            };
        }

        private async Task<ReportDataDto> GetCampaignReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Campaign>()
                .Where(c => !c.IsDeleted)
                .Include(c => c.CreatedByPersonel)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(c => c.StartDate >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(c => c.EndDate <= filter.EndDate.Value);
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(c => c.Status == filter.Status);

            var campaigns = await query.ToListAsync();

            // Lead'leri kampanya bazında getir
            var leads = await _unitOfWork.Query<Lead>()
                .Where(l => !l.IsDeleted && l.CampaignId.HasValue)
                .ToListAsync();

            var items = campaigns.Select(c => new Dictionary<string, object>
            {
                ["Kampanya"] = c.Name,
                ["Tip"] = c.Type ?? "Belirsiz",
                ["Başlangıç"] = c.StartDate.ToString("dd.MM.yyyy"),
                ["Bitiş"] = c.EndDate.ToString("dd.MM.yyyy"),
                ["Bütçe"] = c.Budget.ToString("N2"),
                ["Gerçekleşen"] = c.ActualCost?.ToString("N2") ?? "-",
                ["Hedef Lead"] = c.TargetLeads ?? 0,
                ["Dönüşen Lead"] = leads.Count(l => l.CampaignId == c.Id && l.ConvertedToCustomer != null),
                ["Dönüşüm Oranı"] = (c.TargetLeads ?? 0) > 0
                    ? Math.Round((decimal)leads.Count(l => l.CampaignId == c.Id && l.ConvertedToCustomer != null) / (c.TargetLeads ?? 1) * 100, 2)
                    : 0,
                ["Durum"] = c.Status ?? "Belirsiz",
                ["Oluşturan"] = c.CreatedByPersonel != null ? $"{c.CreatedByPersonel.FirstName} {c.CreatedByPersonel.LastName}" : "Sistem"
            }).ToList();

            var summary = new ReportSummaryDto
            {
                TotalRecords = items.Count
            };

            return new ReportDataDto
            {
                Title = "Kampanya Raporu",
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = items,
                TotalCount = items.Count
            };
        }

        private async Task<ReportDataDto> GetTicketCategoryReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Ticket>()
                .Where(t => !t.IsDeleted)
                .AsQueryable();

            query = ApplyTicketFilters(query, filter);
            var tickets = await query.ToListAsync();

            var categoryData = tickets
                .GroupBy(t => t.Category ?? "Belirsiz")
                .Select(g => new Dictionary<string, object>
                {
                    ["Kategori"] = g.Key,
                    ["Toplam"] = g.Count(),
                    ["Açık"] = g.Count(t => t.Status == "Açık"),
                    ["İşlemde"] = g.Count(t => t.Status == "İşlemde"),
                    ["Çözüldü"] = g.Count(t => t.Status == "Çözüldü"),
                    ["Kapandı"] = g.Count(t => t.Status == "Kapandı")
                })
                .ToList();

            var summary = new ReportSummaryDto
            {
                TotalRecords = categoryData.Count,
                CategoryDistribution = tickets.GroupBy(t => t.Category ?? "Belirsiz")
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return new ReportDataDto
            {
                Title = "Ticket Kategori Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = categoryData,
                TotalCount = categoryData.Count
            };
        }

        private async Task<ReportDataDto> GetTicketPriorityReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Ticket>()
                .Where(t => !t.IsDeleted)
                .AsQueryable();

            query = ApplyTicketFilters(query, filter);
            var tickets = await query.ToListAsync();

            var priorityData = tickets
                .GroupBy(t => t.Priority ?? "Belirsiz")
                .Select(g => new Dictionary<string, object>
                {
                    ["Öncelik"] = g.Key,
                    ["Toplam"] = g.Count(),
                    ["Ortalama Çözüm Süresi (saat)"] = g.Where(t => t.ResolvedAt.HasValue)
                        .Select(t => (t.ResolvedAt.Value - t.CreatedAt).TotalHours)
                        .DefaultIfEmpty(0)
                        .Average()
                        .ToString("F2"),
                    ["Çözülen"] = g.Count(t => t.Status == "Çözüldü"),
                    ["Açık"] = g.Count(t => t.Status == "Açık")
                })
                .ToList();

            var summary = new ReportSummaryDto
            {
                TotalRecords = priorityData.Count
            };

            return new ReportDataDto
            {
                Title = "Ticket Öncelik Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = priorityData,
                TotalCount = priorityData.Count
            };
        }

        private async Task<ReportDataDto> GetLeadSourceReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Lead>()
                .Where(l => !l.IsDeleted)
                .Include(l => l.AssignedToPersonel)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.CreatedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(l => l.CreatedAt <= filter.EndDate.Value);

            var leads = await query.ToListAsync();

            var sourceData = leads
                .GroupBy(l => l.Source ?? "Belirsiz")
                .Select(g => new Dictionary<string, object>
                {
                    ["Kaynak"] = g.Key,
                    ["Toplam"] = g.Count(),
                    ["Müşteri Oldu"] = g.Count(l => l.ConvertedToCustomer != null),
                    ["Dönüşüm Oranı"] = g.Count() > 0
                        ? Math.Round((decimal)g.Count(l => l.ConvertedToCustomer != null) / g.Count() * 100, 2)
                        : 0
                })
                .ToList();

            var summary = new ReportSummaryDto
            {
                TotalRecords = sourceData.Count
            };

            return new ReportDataDto
            {
                Title = "Lead Kaynak Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = sourceData,
                TotalCount = sourceData.Count
            };
        }

        private async Task<ReportDataDto> GetCustomerReportAsync(ReportFilterDto filter)
        {
            var query = _unitOfWork.Query<Customer>()
                .Where(c => !c.IsDeleted)
                .Include(c => c.Tickets)
                .Include(c => c.Invoices)
                .Include(c => c.Orders)
                .Include(c => c.Contracts)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(c => c.CreatedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(c => c.CreatedAt <= filter.EndDate.Value);
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(c => c.Status == filter.Status);
            if (filter.PersonelId.HasValue)
                query = query.Where(c => c.AssignedToPersonelId == filter.PersonelId.Value);

            var customers = await query.ToListAsync();

            var items = customers.Select(c => new Dictionary<string, object>
            {
                ["Müşteri"] = $"{c.FirstName} {c.LastName}",
                ["Email"] = c.Email,
                ["Telefon"] = c.Phone,
                ["Şirket"] = c.CompanyName ?? "-",
                ["Tip"] = c.CustomerType ?? "Belirsiz",
                ["Durum"] = c.Status ?? "Belirsiz",
                ["Toplam Ticket"] = c.Tickets?.Count ?? 0,
                ["Toplam Fatura"] = c.Invoices?.Count ?? 0,
                ["Toplam Sipariş"] = c.Orders?.Count ?? 0,
                ["Toplam Sözleşme"] = c.Contracts?.Count ?? 0,
                ["Oluşturma"] = c.CreatedAt.ToString("dd.MM.yyyy HH:mm")
            }).ToList();

            var summary = new ReportSummaryDto
            {
                TotalRecords = items.Count
            };

            return new ReportDataDto
            {
                Title = "Müşteri Raporu",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,
                Filters = filter,
                Summary = summary,
                Items = items,
                TotalCount = items.Count
            };
        }

        private async Task<ReportDataDto> GetFinancialReportAsync(ReportFilterDto filter)
        {
            var invoiceQuery = _unitOfWork.Query<Invoice>()
                .Where(i => !i.IsDeleted)
                .Include(i => i.Customer)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                invoiceQuery = invoiceQuery.Where(i => i.InvoiceDate >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                invoiceQuery = invoiceQuery.Where(i => i.InvoiceDate <= filter.EndDate.Value);

            var invoices = await invoiceQuery.ToListAsync();

            var paymentQuery = _unitOfWork.Query<Payment>()
                .Where(p => !p.IsDeleted)
                .Include(p => p.Invoice)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                paymentQuery = paymentQuery.Where(p => p.PaymentDate >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                paymentQuery = paymentQuery.Where(p => p.PaymentDate <= filter.EndDate.Value);

            var payments = await paymentQuery.ToListAsync();

            var items = new List<Dictionary<string, object>>
            {
                new() {
                    ["Toplam Fatura"] = invoices.Count,
                    ["Toplam Fatura Tutarı"] = invoices.Sum(i => i.TotalAmount).ToString("N2"),
                    ["Ödenen Tutar"] = payments.Sum(p => p.Amount).ToString("N2"),
                    ["Kalan Tutar"] = (invoices.Sum(i => i.TotalAmount) - payments.Sum(p => p.Amount)).ToString("N2"),
                    ["Ödenmemiş Fatura"] = invoices.Count(i => i.Status != "Ödendi" && i.Status != "İptal"),
                    ["Toplam Ödeme"] = payments.Count
                }
            };

            var summary = new ReportSummaryDto
            {
                TotalRecords = items.Count,
                TotalAmount = invoices.Sum(i => i.TotalAmount),
                Extra = new Dictionary<string, object>
                {
                    ["Ödenen"] = payments.Sum(p => p.Amount),
                    ["Kalan"] = invoices.Sum(i => i.TotalAmount) - payments.Sum(p => p.Amount)
                }
            };

            return new ReportDataDto
            {
                Title = "Finansal Rapor",
                ReportType = filter.Type.ToString(),
                ReportDate = DateTime.UtcNow,  
                Filters = filter,
                Summary = summary,
                Items = items,
                TotalCount = items.Count
            };
        }

        // ==================== Helper Metodlar ====================

        private IQueryable<Ticket> ApplyTicketFilters(IQueryable<Ticket> query, ReportFilterDto filter)
        {
            if (filter.StartDate.HasValue)
                query = query.Where(t => t.CreatedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(t => t.CreatedAt <= filter.EndDate.Value);
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(t => t.Status == filter.Status);
            if (!string.IsNullOrEmpty(filter.Priority))
                query = query.Where(t => t.Priority == filter.Priority);
            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(t => t.Category == filter.Category);
            if (filter.PersonelId.HasValue)
                query = query.Where(t => t.AssignedToPersonelId == filter.PersonelId.Value);
            if (filter.CustomerId.HasValue)
                query = query.Where(t => t.CustomerId == filter.CustomerId.Value);

            return query;
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Az önce";
            if (timeSpan.TotalMinutes < 60)
                return $"{Math.Floor(timeSpan.TotalMinutes)} dakika önce";
            if (timeSpan.TotalHours < 24)
                return $"{Math.Floor(timeSpan.TotalHours)} saat önce";
            if (timeSpan.TotalDays < 30)
                return $"{Math.Floor(timeSpan.TotalDays)} gün önce";
            if (timeSpan.TotalDays < 365)
                return $"{Math.Floor(timeSpan.TotalDays / 30)} ay önce";

            return $"{Math.Floor(timeSpan.TotalDays / 365)} yıl önce";
        }
    }
}