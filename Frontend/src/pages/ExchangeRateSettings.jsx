import { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';

export default function ExchangeRateSettings() {
  const { user } = useAuth();
  const [providers, setProviders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingProvider, setEditingProvider] = useState(null);
  const [currentRates, setCurrentRates] = useState(null);
  const [ratesLoading, setRatesLoading] = useState(false);
  const [formData, setFormData] = useState({
    provider: '',
    name: '',
    apiUrl: '',
    apiKey: '',
    priority: 1,
    cacheDurationMinutes: 60
  });

  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';

  useEffect(() => {
    fetchProviders();
    fetchCurrentRates();
  }, []);

  const fetchProviders = async () => {
    try {
      setLoading(true);
      const response = await api.get('/ExchangeRateSettings');
      setProviders(response.data || []);
    } catch (error) {
      toast.error('Kur servisleri yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const fetchCurrentRates = async () => {
    try {
      setRatesLoading(true);
      
      const [usdRes, eurRes, gbpRes] = await Promise.all([
        api.get('/ExchangeRateSettings/rate/USD/TRY'),
        api.get('/ExchangeRateSettings/rate/EUR/TRY'),
        api.get('/ExchangeRateSettings/rate/GBP/TRY')
      ]);

      // 1. ⚡️ DÜZELTME: .data yerine .data.rate diyerek nesnenin içindeki sayıyı alıyoruz (NaN çözüldü)
      setCurrentRates({
        usd: usdRes.data.rate,
        eur: eurRes.data.rate,
        gbp: gbpRes.data.rate,
        updatedAt: new Date()
      });

      // 2. ⚡️ UYARI SİSTEMİ: Eğer backend 'isDefault = true' döndüyse kullanıcıyı dürüstçe uyarıyoruz
      const isAnyDefault = usdRes.data.isDefault || eurRes.data.isDefault || gbpRes.data.isDefault;
      
      if (isAnyDefault) {
        toast((t) => (
          <span>
            ⚠️ <b>API Bağlantı Hatası!</b><br/>
            Seçili kur sağlayıcıya erişilemedi. Sistemde kayıtlı olan <b>varsayılan (default)</b> değerler getirildi.
          </span>
        ), {
          duration: 6000, // Kullanıcı rahat okusun diye 6 saniye kalacak kanka
          position: 'top-right',
          style: {
            background: '#FEE2E2', // Açık kırmızı/turuncu tonu arıza hissettirsin diye
            color: '#991B1B',
            border: '1px solid #F87171',
          }
        });
      }

    } catch (error) {
      console.error('Kurlar alınamadı:', error);
      toast.error('Sistem kurları çekemedi.');
    } finally {
      setRatesLoading(false);
    }
  };

  const handleSwitchProvider = async (id, name) => {
    const result = await Swal.fire({
      title: 'Servisi Aktif Et',
      text: `${name} kur servisini aktif etmek istediğinize emin misiniz?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Aktif Et',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        await api.post(`/ExchangeRateSettings/switch/${id}`);
        toast.success(`${name} kur servisi aktif edildi`);
        fetchProviders();
        fetchCurrentRates();
      } catch (error) {
        toast.error(error.response?.data?.message || 'Değiştirme hatası');
      }
    }
  };

  const handleDelete = async (id, name, isActive) => {
    if (isActive) {
      toast.error('Aktif olan bir servis silinemez. Önce başka bir servisi aktif edin.');
      return;
    }

    const result = await Swal.fire({
      title: 'Servisi Sil',
      text: `${name} kur servisini silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, Sil',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        await api.delete(`/ExchangeRateSettings/${id}`);
        toast.success('Kur servisi silindi');
        fetchProviders();
      } catch (error) {
        toast.error(error.response?.data?.message || 'Silme hatası');
      }
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    try {
      if (editingProvider) {
        await api.put(`/ExchangeRateSettings/${editingProvider.id}`, {
          name: formData.name,
          apiUrl: formData.apiUrl,
          apiKey: formData.apiKey,
          priority: parseInt(formData.priority),
          cacheDurationMinutes: parseInt(formData.cacheDurationMinutes)
        });
        toast.success('Kur servisi güncellendi');
      } else {
        await api.post('/ExchangeRateSettings', formData);
        toast.success('Yeni kur servisi eklendi');
      }
      setShowModal(false);
      setEditingProvider(null);
      resetForm();
      fetchProviders();
    } catch (error) {
      toast.error(error.response?.data?.message || 'İşlem hatası');
    }
  };

  const handleEdit = (provider) => {
    setEditingProvider(provider);
    setFormData({
      provider: provider.provider,
      name: provider.name,
      apiUrl: provider.apiUrl || '',
      apiKey: provider.apiKey || '',
      priority: provider.priority,
      cacheDurationMinutes: provider.cacheDurationMinutes || 60
    });
    setShowModal(true);
  };

  const resetForm = () => {
    setFormData({
      provider: '',
      name: '',
      apiUrl: '',
      apiKey: '',
      priority: 1,
      cacheDurationMinutes: 60
    });
  };

  const getProviderIcon = (provider) => {
    const icons = {
      'Tcmb': '🏦',
      'OpenExchangeRates': '🌐',
      'Fixer': '💰',
      'CurrencyAPI': '💱'
    };
    return icons[provider] || '📊';
  };

  const getStatusBadge = (isActive) => {
    return isActive ? (
      <span className="px-2 py-1 rounded-full text-xs bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300">
        ✅ Aktif
      </span>
    ) : (
      <span className="px-2 py-1 rounded-full text-xs bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300">
        ⏸ Pasif
      </span>
    );
  };

  const formatPrice = (price) => {
    if (!price) return '-';
    return new Intl.NumberFormat('tr-TR', {
      minimumFractionDigits: 4,
      maximumFractionDigits: 4
    }).format(price);
  };

  if (!isAdmin) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <div className="text-6xl mb-4">🔒</div>
          <h2 className="text-2xl font-bold text-gray-800 dark:text-white mb-2">Yetkiniz Yok</h2>
          <p className="text-gray-500 dark:text-gray-400">Bu sayfaya erişim yetkiniz bulunmamaktadır.</p>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Kur Servisi Ayarları</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">Döviz kuru sağlayıcılarını yönetin</p>
        </div>
        <button
          onClick={() => {
            setEditingProvider(null);
            resetForm();
            setShowModal(true);
          }}
          className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg transition-colors"
        >
          + Yeni Servis Ekle
        </button>
      </div>

      {/* Güncel Kurlar Kartı */}
      <div className="bg-gradient-to-r from-blue-500 to-indigo-600 rounded-2xl shadow-lg p-6 mb-8 text-white">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text-lg font-semibold">📊 Güncel Döviz Kurları</h2>
            <p className="text-blue-100 text-sm mt-1">Aktif kur servisi üzerinden alınan güncel kurlar</p>
          </div>
          <button
            onClick={fetchCurrentRates}
            disabled={ratesLoading}
            className="px-3 py-1.5 bg-white/20 hover:bg-white/30 rounded-lg text-sm transition-colors flex items-center gap-2"
          >
            {ratesLoading ? (
              <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
            ) : (
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
            )}
            <span>Yenile</span>
          </button>
        </div>
        
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="bg-white/10 backdrop-blur-sm rounded-xl p-4">
            <div className="flex items-center justify-between mb-2">
              <span className="text-2xl">🇺🇸</span>
              <span className="text-xs text-blue-200">USD/TRY</span>
            </div>
            <div className="text-2xl font-bold">
              {ratesLoading ? (
                <div className="w-6 h-6 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
              ) : (
                `${formatPrice(currentRates?.usd)} ₺`
              )}
            </div>
            <p className="text-blue-100 text-xs mt-2">1 Amerikan Doları</p>
          </div>
          
          <div className="bg-white/10 backdrop-blur-sm rounded-xl p-4">
            <div className="flex items-center justify-between mb-2">
              <span className="text-2xl">🇪🇺</span>
              <span className="text-xs text-blue-200">EUR/TRY</span>
            </div>
            <div className="text-2xl font-bold">
              {ratesLoading ? (
                <div className="w-6 h-6 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
              ) : (
                `${formatPrice(currentRates?.eur)} ₺`
              )}
            </div>
            <p className="text-blue-100 text-xs mt-2">1 Euro</p>
          </div>
          
          <div className="bg-white/10 backdrop-blur-sm rounded-xl p-4">
            <div className="flex items-center justify-between mb-2">
              <span className="text-2xl">🇬🇧</span>
              <span className="text-xs text-blue-200">GBP/TRY</span>
            </div>
            <div className="text-2xl font-bold">
              {ratesLoading ? (
                <div className="w-6 h-6 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
              ) : (
                `${formatPrice(currentRates?.gbp)} ₺`
              )}
            </div>
            <p className="text-blue-100 text-xs mt-2">1 İngiliz Sterlini</p>
          </div>
        </div>
        
        {currentRates?.updatedAt && (
          <p className="text-blue-100 text-xs mt-4 text-right">
            Son güncelleme: {currentRates.updatedAt.toLocaleTimeString('tr-TR')}
          </p>
        )}
      </div>

      {/* Servisler Tablosu */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-700">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Servis</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">API URL</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Durum</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Öncelik</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">İşlemler</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {providers.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-gray-500">
                    Henüz kur servisi eklenmemiş
                  </td>
                </tr>
              ) : (
                providers.map((provider) => (
                  <tr key={provider.id} className="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        <span className="text-xl">{getProviderIcon(provider.provider)}</span>
                        <div>
                          <p className="font-medium text-gray-800 dark:text-white">{provider.name}</p>
                          <p className="text-xs text-gray-500">{provider.provider}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <p className="text-sm text-gray-600 dark:text-gray-300 font-mono truncate max-w-xs">
                        {provider.apiUrl || '-'}
                      </p>
                      {provider.apiKey && (
                        <p className="text-xs text-gray-400">API Key: ••••••••</p>
                      )}
                    </td>
                    <td className="px-6 py-4">{getStatusBadge(provider.isActive)}</td>
                    <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-300">{provider.priority}</td>
                    <td className="px-6 py-4 text-center">
                      <div className="flex justify-center gap-2">
                        {!provider.isActive && (
                          <button
                            onClick={() => handleSwitchProvider(provider.id, provider.name)}
                            className="px-3 py-1 text-xs bg-green-600 hover:bg-green-700 text-white rounded-lg transition-colors"
                          >
                            Aktif Et
                          </button>
                        )}
                        <button
                          onClick={() => handleEdit(provider)}
                          className="px-3 py-1 text-xs bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors"
                        >
                          Düzenle
                        </button>
                        <button
                          onClick={() => handleDelete(provider.id, provider.name, provider.isActive)}
                          className="px-3 py-1 text-xs bg-red-600 hover:bg-red-700 text-white rounded-lg transition-colors"
                        >
                          Sil
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-md">
            <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
              <h2 className="text-xl font-bold text-gray-800 dark:text-white">
                {editingProvider ? 'Servis Düzenle' : 'Yeni Servis Ekle'}
              </h2>
            </div>
            <form onSubmit={handleSubmit} className="p-6">
              <div className="space-y-4">
                {!editingProvider && (
                  <>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                        Servis Kodu <span className="text-red-500">*</span>
                      </label>
                      <input
                        type="text"
                        placeholder="Örn: Tcmb, OpenExchangeRates"
                        value={formData.provider}
                        onChange={(e) => setFormData({ ...formData, provider: e.target.value })}
                        className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600"
                        required
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                        Servis Adı <span className="text-red-500">*</span>
                      </label>
                      <input
                        type="text"
                        placeholder="Örn: TCMB"
                        value={formData.name}
                        onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                        className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600"
                        required
                      />
                    </div>
                  </>
                )}
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    API URL <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    placeholder="https://..."
                    value={formData.apiUrl}
                    onChange={(e) => setFormData({ ...formData, apiUrl: e.target.value })}
                    className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    API Key (varsa)
                  </label>
                  <input
                    type="text"
                    placeholder="API Key"
                    value={formData.apiKey}
                    onChange={(e) => setFormData({ ...formData, apiKey: e.target.value })}
                    className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600"
                  />
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Öncelik
                    </label>
                    <input
                      type="number"
                      min="1"
                      value={formData.priority}
                      onChange={(e) => setFormData({ ...formData, priority: parseInt(e.target.value) || 1 })}
                      className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Cache Süresi (dk)
                    </label>
                    <input
                      type="number"
                      min="5"
                      value={formData.cacheDurationMinutes}
                      onChange={(e) => setFormData({ ...formData, cacheDurationMinutes: parseInt(e.target.value) || 60 })}
                      className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600"
                    />
                  </div>
                </div>
              </div>
              <div className="flex justify-end gap-3 mt-6 pt-4 border-t border-gray-200 dark:border-gray-700">
                <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border rounded-lg hover:bg-gray-100">
                  İptal
                </button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
                  {editingProvider ? 'Güncelle' : 'Ekle'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Bilgi Kartı */}
      <div className="mt-6 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800/30">
        <div className="flex items-start gap-3">
          <span className="text-xl">💡</span>
          <div>
            <h3 className="font-semibold text-blue-800 dark:text-blue-300">Bilgi</h3>
            <p className="text-sm text-blue-700 dark:text-blue-400 mt-1">
              • Sadece bir kur servisi aktif olabilir.<br />
              • Aktif servis değiştiğinde yeni siparişlerde güncel kurlar kullanılır.<br />
              • Mevcut siparişler etkilenmez.<br />
              • Değişiklik anında aktiftir, uygulama yeniden başlatılmasına gerek yoktur.<br />
              • Güncel kurlar her sayfa yenilemede otomatik güncellenir.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}