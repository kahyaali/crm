import { useState, useEffect, useCallback, useRef } from 'react';
import { usePermissions } from '../hooks/usePermissions';
import { PERMISSIONS } from '../constants/permissions';
import api from '../services/api';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  FaFileExcel, FaFilePdf, FaSync, FaFilter, 
  FaTimes, FaSearch, FaChevronLeft, FaChevronRight, 
  FaPrint, FaFileAlt, FaCalendarAlt, FaUser, 
  FaBuilding, FaTag, FaClock, FaMoneyBillWave,
  FaChartBar, FaTable
} from 'react-icons/fa';
import { toast } from 'react-hot-toast';

export default function ReportPage() {
  const { hasPermission } = usePermissions();
  const canExport = hasPermission(PERMISSIONS.EXPORT_REPORTS);
  const canViewReports = hasPermission(PERMISSIONS.VIEW_REPORTS);

  const [personelList, setPersonelList] = useState([]);
  const [customerList, setCustomerList] = useState([]);
  const [loading, setLoading] = useState(false);
  const [reportData, setReportData] = useState(null);
  const [error, setError] = useState(null);
  const [reportTypes, setReportTypes] = useState([]);
  const [showFilters, setShowFilters] = useState(true);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 50,
    totalPages: 1,
    totalRecords: 0,
  });

  const [filters, setFilters] = useState({
    type: 1,
    startDate: '',
    endDate: '',
    status: '',
    personelId: '',
    customerId: '',
    campaignId: '',
    priority: '',
    category: '',
    source: '',
    stage: '',
    searchTerm: '',
  });

  const abortControllerRef = useRef(null);
  const isFetchingRef = useRef(false);
  const fetchTimeoutRef = useRef(null);

  const handleFilterChange = (key, value) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  };

  const handleTypeChange = (e) => {
    handleFilterChange('type', parseInt(e.target.value));
  };

  // Rapor tiplerini getir
  useEffect(() => {
    const fetchReportTypes = async () => {
      try {
        const response = await api.get('/Report/types');
        setReportTypes(response.data);
      } catch (error) {
        setReportTypes([
          { value: 1, name: 'Ticket Durum Raporu' },
          { value: 2, name: 'Personel Performans Raporu' },
          { value: 3, name: 'Ticket Kategori Raporu' },
          { value: 4, name: 'Ticket Öncelik Raporu' },
          { value: 10, name: 'Lead Durum Raporu' },
          { value: 11, name: 'Lead Dönüşüm Raporu' },
          { value: 12, name: 'Lead Kaynak Raporu' },
          { value: 20, name: 'Fırsat Raporu' },
          { value: 21, name: 'Gelir Raporu' },
          { value: 30, name: 'Müşteri Raporu' },
          { value: 40, name: 'Personel Performans Raporu' },
          { value: 50, name: 'Finansal Rapor' },
          { value: 60, name: 'Kampanya Raporu' },
        ]);
      }
    };
    fetchReportTypes();
  }, []);

  // Personel listesini getir
  useEffect(() => {
    const fetchPersonels = async () => {
      try {
        const response = await api.get('/Report/personels');
        setPersonelList(response.data || []);
      } catch (error) {
        console.error('Personel listesi alınamadı:', error);
        setPersonelList([]);
      }
    };
    fetchPersonels();
  }, []);

  // Müşteri listesini getir
  useEffect(() => {
    const fetchCustomers = async () => {
      try {
        const response = await api.get('/Report/customers');
        setCustomerList(response.data || []);
      } catch (error) {
        console.error('Müşteri listesi alınamadı:', error);
        setCustomerList([]);
      }
    };
    fetchCustomers();
  }, []);

  // Rapor oluştur
  const generateReport = useCallback(async (page = 1) => {
    if (!canViewReports) {
      toast.error('Rapor görüntüleme yetkiniz yok!');
      return;
    }

    if (isFetchingRef.current) {
      console.log('⏳ Zaten rapor oluşturuluyor...');
      return;
    }

    if (fetchTimeoutRef.current) {
      clearTimeout(fetchTimeoutRef.current);
    }

    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    fetchTimeoutRef.current = setTimeout(async () => {
      const controller = new AbortController();
      abortControllerRef.current = controller;

      try {
        isFetchingRef.current = true;
        setLoading(true);
        setError(null);

        const params = {
          type: filters.type,
          startDate: filters.startDate || null,
          endDate: filters.endDate || null,
          status: filters.status || "",
          personelId: filters.personelId ? parseInt(filters.personelId) : null,
          customerId: filters.customerId ? parseInt(filters.customerId) : null,
          campaignId: filters.campaignId ? parseInt(filters.campaignId) : null,
          priority: filters.priority || "",
          category: filters.category || "",
          source: filters.source || "",
          stage: filters.stage || "",
          searchTerm: filters.searchTerm || "",
          pageNumber: page,
          pageSize: pagination.pageSize,
        };

        console.log('📤 Rapor isteği:', params);

        const response = await api.post('/Report/generate', params, {
          signal: controller.signal,
        });

        const data = response.data.data;
        console.log('📊 Rapor verisi:', data);

        if (data) {
          setReportData(data);
          setPagination(prev => ({
            ...prev,
            page: data.pageNumber || page,
            totalPages: data.totalPages || 1,
            totalRecords: data.totalCount || data.items?.length || 0,
          }));
          toast.success('Rapor başarıyla oluşturuldu!');
        } else {
          setError('Veri gelmedi!');
          toast.error('Rapor verisi alınamadı!');
        }
      } catch (error) {
        if (error.name === 'AbortError' || error.code === 'ERR_CANCELED') {
          console.log('⏹️ İstek iptal edildi');
          return;
        }
        console.error('❌ Rapor hatası:', error);
        if (error.response?.data) {
          toast.error(`Hata: ${error.response.data.message || 'Geçersiz parametreler'}`);
        } else {
          toast.error('Rapor oluşturulamadı!');
        }
        setError('Rapor oluşturulurken bir hata oluştu.');
      } finally {
        isFetchingRef.current = false;
        setLoading(false);
        abortControllerRef.current = null;
        fetchTimeoutRef.current = null;
      }
    }, 300);
  }, [filters, pagination.pageSize, canViewReports]);

  // İlk yükleme
  useEffect(() => {
    generateReport(1);
    return () => {
      if (fetchTimeoutRef.current) clearTimeout(fetchTimeoutRef.current);
      if (abortControllerRef.current) abortControllerRef.current.abort();
      isFetchingRef.current = false;
    };
  }, [filters.type]);

  // Export işlemleri
const handleExport = async (type) => {
  if (!canExport) {
    toast.error('Export yetkiniz yok!');
    return;
  }

  try {
    // 🔥 Export için temiz veri gönder
    const exportData = {
      type: filters.type,  // 1, 2, 3 gibi sayısal değer
      startDate: filters.startDate || null,
      endDate: filters.endDate || null,
      status: filters.status || "",
      personelId: filters.personelId ? parseInt(filters.personelId) : null,
      customerId: filters.customerId ? parseInt(filters.customerId) : null,
      campaignId: filters.campaignId ? parseInt(filters.campaignId) : null,
      priority: filters.priority || "",
      category: filters.category || "",
      source: filters.source || "",
      stage: filters.stage || "",
      searchTerm: filters.searchTerm || "",
      pageNumber: 1,
      pageSize: 1000, // Export için tüm verileri al
    };

    console.log('📤 Export isteği:', exportData);

    const response = await api.post(`/Report/${type}`, exportData, {
      responseType: 'blob',
      timeout: 120000,
    });

    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `Rapor_${new Date().toISOString().slice(0,10)}.${type === 'excel' ? 'xlsx' : 'pdf'}`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);

    toast.success(`${type.toUpperCase()} başarıyla indirildi!`);
  } catch (error) {
    console.error('❌ Export hatası:', error);
    if (error.response?.status === 400) {
      toast.error('Export parametreleri geçersiz!');
    } else if (error.response?.status === 429) {
      toast.error('Çok fazla istek! Lütfen bekleyin.');
    } else {
      toast.error(`${type.toUpperCase()} indirilemedi!`);
    }
  }
};

  const resetFilters = () => {
    setFilters({
      type: filters.type,
      startDate: '',
      endDate: '',
      status: '',
      personelId: '',
      customerId: '',
      campaignId: '',
      priority: '',
      category: '',
      source: '',
      stage: '',
      searchTerm: '',
    });
    setError(null);
    toast.info('Filtreler sıfırlandı');
  };

  const handlePageChange = (newPage) => {
    if (newPage >= 1 && newPage <= pagination.totalPages && !isFetchingRef.current) {
      generateReport(newPage);
    }
  };

  const getReportTypeLabel = () => {
    const found = reportTypes.find(t => t.value === filters.type);
    return found?.name || filters.type;
  };

  if (!canViewReports) {
    return (
      <div className="flex flex-col items-center justify-center h-[60vh]">
        <div className="text-6xl mb-4">🔒</div>
        <h2 className="text-2xl font-semibold text-gray-700 dark:text-gray-300">Yetkiniz Yok</h2>
        <p className="text-gray-500 dark:text-gray-400 mt-2">Raporları görüntüleme yetkiniz bulunmuyor.</p>
      </div>
    );
  }

  const safeData = reportData || {};
  const hasData = safeData && Object.keys(safeData).length > 0;

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 p-4 md:p-6">
      <div className="max-w-7xl mx-auto">
        {/* HEADER */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
          <div>
            <h1 className="text-2xl md:text-3xl font-bold text-gray-900 dark:text-white flex items-center gap-3">
              <FaFileAlt className="text-indigo-600" />
              Raporlar
            </h1>
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
              Filtrelerle rapor oluşturun, görüntüleyin ve dışa aktarın
            </p>
          </div>

          <div className="flex items-center gap-2 flex-wrap">
            <button
              onClick={() => setShowFilters(!showFilters)}
              className="px-4 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl hover:bg-gray-50 dark:hover:bg-gray-700 transition-all flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300"
            >
              <FaFilter />
              {showFilters ? 'Filtreleri Gizle' : 'Filtreleri Göster'}
            </button>

            <button
              onClick={() => generateReport(pagination.page)}
              disabled={loading || isFetchingRef.current}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl transition-all flex items-center gap-2 text-sm font-medium shadow-lg shadow-indigo-600/20 disabled:opacity-50"
            >
              <FaSync className={loading ? 'animate-spin' : ''} />
              {loading ? 'Oluşturuluyor...' : 'Rapor Oluştur'}
            </button>

            {canExport && (
              <>
                <button
                  onClick={() => handleExport('excel')}
                  className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl transition-all flex items-center gap-2 text-sm font-medium shadow-lg shadow-emerald-600/20"
                >
                  <FaFileExcel />
                  Excel
                </button>
                <button
                  onClick={() => handleExport('pdf')}
                  className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-xl transition-all flex items-center gap-2 text-sm font-medium shadow-lg shadow-red-600/20"
                >
                  <FaFilePdf />
                  PDF
                </button>
              </>
            )}
          </div>
        </div>

        {/* FİLTRELER */}
        <AnimatePresence>
          {showFilters && (
            <motion.div
              initial={{ opacity: 0, height: 0 }}
              animate={{ opacity: 1, height: 'auto' }}
              exit={{ opacity: 0, height: 0 }}
              className="overflow-hidden mb-6"
            >
              <div className="bg-white dark:bg-gray-800/50 backdrop-blur-sm rounded-xl border border-gray-200 dark:border-gray-700/50 p-5 shadow-sm">
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  {/* Rapor Tipi */}
                  <div>
                    <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">Rapor Tipi</label>
                    <select
                      value={filters.type}
                      onChange={handleTypeChange}
                      className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    >
                      {reportTypes.map((type) => (
                        <option key={type.value} value={type.value}>{type.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Başlangıç Tarihi */}
                  <div>
                    <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                      <FaCalendarAlt className="inline mr-1 text-gray-400" /> Başlangıç Tarihi
                    </label>
                    <input
                      type="date"
                      value={filters.startDate}
                      onChange={(e) => handleFilterChange('startDate', e.target.value)}
                      className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>

                  {/* Bitiş Tarihi */}
                  <div>
                    <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                      <FaCalendarAlt className="inline mr-1 text-gray-400" /> Bitiş Tarihi
                    </label>
                    <input
                      type="date"
                      value={filters.endDate}
                      onChange={(e) => handleFilterChange('endDate', e.target.value)}
                      className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>

                  {/* Durum */}
                  <div>
                    <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                      <FaTag className="inline mr-1 text-gray-400" /> Durum
                    </label>
                    <select
                      value={filters.status}
                      onChange={(e) => handleFilterChange('status', e.target.value)}
                      className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    >
                      <option value="">Tümü</option>
                      <option value="Açık">Açık</option>
                      <option value="İşlemde">İşlemde</option>
                      <option value="Beklemede">Beklemede</option>
                      <option value="Çözüldü">Çözüldü</option>
                      <option value="Kapandı">Kapandı</option>
                    </select>
                  </div>

                  {/* Personel - Veritabanından Çekiliyor */}
                  <div>
                    <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                      <FaUser className="inline mr-1 text-gray-400" /> Personel
                    </label>
                    <select
                      value={filters.personelId}
                      onChange={(e) => handleFilterChange('personelId', e.target.value)}
                      className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    >
                      <option value="">Tümü</option>
                      {personelList.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Müşteri - Veritabanından Çekiliyor */}
                  <div>
                    <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                      <FaBuilding className="inline mr-1 text-gray-400" /> Müşteri
                    </label>
                    <select
                      value={filters.customerId}
                      onChange={(e) => handleFilterChange('customerId', e.target.value)}
                      className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    >
                      <option value="">Tümü</option>
                      {customerList.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Öncelik */}
                  <div>
                    <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                      <FaClock className="inline mr-1 text-gray-400" /> Öncelik
                    </label>
                    <select
                      value={filters.priority}
                      onChange={(e) => handleFilterChange('priority', e.target.value)}
                      className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    >
                      <option value="">Tümü</option>
                      <option value="Düşük">Düşük</option>
                      <option value="Orta">Orta</option>
                      <option value="Yüksek">Yüksek</option>
                      <option value="Acil">Acil</option>
                    </select>
                  </div>

                  {/* Kategori */}
                  <div>
                    <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                      <FaBuilding className="inline mr-1 text-gray-400" /> Kategori
                    </label>
                    <select
                      value={filters.category}
                      onChange={(e) => handleFilterChange('category', e.target.value)}
                      className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    >
                      <option value="">Tümü</option>
                      <option value="Şikayet">Şikayet</option>
                      <option value="Talep">Talep</option>
                      <option value="Bilgi">Bilgi</option>
                      <option value="Teknik Destek">Teknik Destek</option>
                    </select>
                  </div>

                  {/* Kaynak */}
                  <div>
                    <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                      <FaSearch className="inline mr-1 text-gray-400" /> Kaynak
                    </label>
                    <select
                      value={filters.source}
                      onChange={(e) => handleFilterChange('source', e.target.value)}
                      className="w-full px-3 py-2 bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    >
                      <option value="">Tümü</option>
                      <option value="Web">Web</option>
                      <option value="Referans">Referans</option>
                      <option value="Reklam">Reklam</option>
                      <option value="Fuar">Fuar</option>
                      <option value="Sosyal Medya">Sosyal Medya</option>
                    </select>
                  </div>
                </div>

                {/* Filtre Aksiyonları */}
                <div className="flex items-center justify-end gap-3 mt-4 pt-4 border-t border-gray-100 dark:border-gray-700">
                  <button
                    onClick={resetFilters}
                    className="px-4 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-all flex items-center gap-2 text-sm"
                  >
                    <FaTimes /> Filtreleri Sıfırla
                  </button>
                  <button
                    onClick={() => generateReport(1)}
                    disabled={loading || isFetchingRef.current}
                    className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg transition-all flex items-center gap-2 text-sm disabled:opacity-50"
                  >
                    <FaSearch /> Rapor Oluştur
                  </button>
                </div>
              </div>
            </motion.div>
          )}
        </AnimatePresence>

        {/* RAPOR SONUÇLARI */}
        {loading ? (
          <div className="flex justify-center items-center h-96">
            <div className="relative">
              <div className="animate-spin rounded-full h-16 w-16 border-4 border-indigo-500/20 border-t-indigo-600"></div>
              <div className="absolute inset-0 flex items-center justify-center">
                <span className="text-xs font-medium text-indigo-600 animate-pulse">RAPOR HAZIRLANIYOR</span>
              </div>
            </div>
          </div>
        ) : error ? (
          <div className="flex flex-col items-center justify-center h-96 bg-white dark:bg-gray-800/50 rounded-xl border border-gray-200 dark:border-gray-700/50 p-6">
            <div className="text-6xl mb-4">⚠️</div>
            <h3 className="text-lg font-medium text-gray-700 dark:text-gray-300">Bir Hata Oluştu</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-2 text-center max-w-md">{error}</p>
            <button
              onClick={() => generateReport(1)}
              disabled={loading || isFetchingRef.current}
              className="mt-4 px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition disabled:opacity-50 flex items-center gap-2"
            >
              <FaSync className={loading ? 'animate-spin' : ''} />
              {loading ? 'Yenileniyor...' : 'Tekrar Dene'}
            </button>
          </div>
        ) : hasData ? (
          <div className="space-y-6">
            {/* Rapor Başlığı ve Özet */}
            <div className="bg-white dark:bg-gray-800/50 backdrop-blur-sm rounded-xl border border-gray-200 dark:border-gray-700/50 p-5 shadow-sm">
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                  <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
                    {safeData?.title || getReportTypeLabel()}
                  </h2>
                  <p className="text-sm text-gray-500 dark:text-gray-400">
                    Oluşturma Tarihi: {safeData?.reportDate || 'Tarih bilgisi yok'}
                  </p>
                </div>
                <div className="flex items-center gap-4 text-sm">
                  <span className="text-gray-500 dark:text-gray-400">
                    Toplam Kayıt: <span className="font-semibold text-gray-900 dark:text-white">{safeData?.totalCount || 0}</span>
                  </span>
                  <span className="text-gray-500 dark:text-gray-400">
                    Sayfa: <span className="font-semibold text-gray-900 dark:text-white">{pagination.page} / {pagination.totalPages}</span>
                  </span>
                </div>
              </div>
            </div>

            {/* Özet Kartları */}
            {safeData?.summary && (
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                {safeData.summary.totalRecords !== undefined && (
                  <SummaryCard icon={<FaTable className="text-blue-500" />} label="Toplam Kayıt" value={safeData.summary.totalRecords} color="blue" />
                )}
                {safeData.summary.totalAmount !== undefined && (
                  <SummaryCard icon={<FaMoneyBillWave className="text-emerald-500" />} label="Toplam Tutar" value={`₺${safeData.summary.totalAmount || 0}`} color="emerald" />
                )}
                {safeData.summary.averageAmount !== undefined && (
                  <SummaryCard icon={<FaChartBar className="text-purple-500" />} label="Ortalama Tutar" value={`₺${safeData.summary.averageAmount || 0}`} color="purple" />
                )}
                {safeData.summary.statusDistribution && (
                  <SummaryCard icon={<FaTag className="text-orange-500" />} label="Durum Dağılımı" value={`${Object.keys(safeData.summary.statusDistribution).length} farklı`} color="orange" />
                )}
              </div>
            )}

            {/* Rapor Tablosu */}
            <div className="bg-white dark:bg-gray-800/50 backdrop-blur-sm rounded-xl border border-gray-200 dark:border-gray-700/50 shadow-sm overflow-hidden">
              <div className="overflow-x-auto">
                {safeData?.items && safeData.items.length > 0 ? (
                  <table className="w-full">
                    <thead className="bg-gray-50 dark:bg-gray-800/80">
                      <tr>
                        {Object.keys(safeData.items[0]).map((header) => (
                          <th key={header} className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider border-b border-gray-200 dark:border-gray-700 whitespace-nowrap">
                            {header}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
                      {safeData.items.map((item, index) => (
                        <tr key={index} className="hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors">
                          {Object.values(item).map((value, i) => (
                            <td key={i} className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 whitespace-nowrap max-w-xs truncate">
                              {value?.toString() || '-'}
                            </td>
                          ))}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                ) : (
                  <div className="text-center py-12">
                    <div className="text-4xl mb-2">📭</div>
                    <p className="text-gray-400 dark:text-gray-500">Rapor verisi bulunamadı</p>
                  </div>
                )}
              </div>
            </div>

            {/* Print Butonu */}
            <div className="flex justify-end">
              <button
                onClick={() => window.print()}
                className="px-4 py-2 bg-gray-600 hover:bg-gray-700 text-white rounded-xl transition-all flex items-center gap-2 text-sm"
              >
                <FaPrint /> Yazdır
              </button>
            </div>
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center h-96 bg-white dark:bg-gray-800/50 rounded-xl border border-gray-200 dark:border-gray-700/50">
            <div className="text-6xl mb-4">📊</div>
            <h3 className="text-lg font-medium text-gray-700 dark:text-gray-300">Rapor Oluşturmak İçin Filtreleri Seçin</h3>
            <p className="text-sm text-gray-400 dark:text-gray-500 mt-1">Filtreleri ayarlayın ve "Rapor Oluştur" butonuna tıklayın</p>
          </div>
        )}
      </div>
    </div>
  );
}

// ============================================================
// ALT COMPONENT
// ============================================================

function SummaryCard({ icon, label, value, color }) {
  const colors = {
    blue: 'bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-900/30',
    emerald: 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-200 dark:border-emerald-900/30',
    purple: 'bg-purple-50 dark:bg-purple-900/20 border-purple-200 dark:border-purple-900/30',
    orange: 'bg-orange-50 dark:bg-orange-900/20 border-orange-200 dark:border-orange-900/30',
    red: 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-900/30',
    indigo: 'bg-indigo-50 dark:bg-indigo-900/20 border-indigo-200 dark:border-indigo-900/30',
  };

  return (
    <div className={`${colors[color] || colors.blue} rounded-xl border p-4 backdrop-blur-sm`}>
      <div className="flex items-center gap-3">
        <div className="text-xl">{icon}</div>
        <div>
          <p className="text-xs font-medium text-gray-500 dark:text-gray-400">{label}</p>
          <p className="text-lg font-bold text-gray-900 dark:text-white">{value}</p>
        </div>
      </div>
    </div>
  );
}