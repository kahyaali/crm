using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Reports
{
    public class ReportDataDto
    {
        /// <summary>
        /// Rapor Başlığı
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Rapor Tipi
        /// </summary>
        public string ReportType { get; set; }

        /// <summary>
        /// Rapor Tarihi
        /// </summary>
        public DateTime ReportDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Kullanılan Filtreler
        /// </summary>
        public ReportFilterDto Filters { get; set; }

        /// <summary>
        /// Rapor Özet Bilgileri (Toplam, Ortalama, vs.)
        /// </summary>
        public ReportSummaryDto Summary { get; set; }

        /// <summary>
        /// Rapor Verileri (Liste)
        /// </summary>
        public List<Dictionary<string, object>> Items { get; set; } = new();

        /// <summary>
        /// Grafik Verileri (Opsiyonel)
        /// </summary>
        public List<ChartDataDto> Charts { get; set; } = new();

        /// <summary>
        /// Toplam Kayıt Sayısı
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Sayfa Numarası (Pagination varsa)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Sayfa Boyutu (Pagination varsa)
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// Toplam Sayfa Sayısı (Pagination varsa)
        /// </summary>
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Rapor Özet Bilgileri
    /// </summary>
    public class ReportSummaryDto
    {
        /// <summary>
        /// Toplam Kayıt
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Toplam Tutar (Finansal raporlar için)
        /// </summary>
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// Ortalama Tutar
        /// </summary>
        public decimal? AverageAmount { get; set; }

        /// <summary>
        /// Minimum Tutar
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Maximum Tutar
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Durum Bazında Dağılım (Ticket, Lead, vs.)
        /// </summary>
        public Dictionary<string, int> StatusDistribution { get; set; } = new();

        /// <summary>
        /// Kategori Bazında Dağılım
        /// </summary>
        public Dictionary<string, int> CategoryDistribution { get; set; } = new();

        /// <summary>
        /// Personel Bazında Dağılım
        /// </summary>
        public Dictionary<string, int> PersonnelDistribution { get; set; } = new();

        /// <summary>
        /// Ekstra Özet Bilgiler (JSON)
        /// </summary>
        public Dictionary<string, object> Extra { get; set; } = new();
    }

    /// <summary>
    /// Grafik Veri Noktası
    /// </summary>
    public class ChartDataDto
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
        public string? Color { get; set; }
        public decimal? Percentage { get; set; }
        public string? Category { get; set; }
        public DateTime? Date { get; set; }
    }
}
