import { useState, useEffect } from 'react';
import { useParams, Link, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import toast from 'react-hot-toast';
import api from '../services/api';

export default function PersonelDetail() {
  const { id } = useParams();
  const location = useLocation();
  const { user, hasPermission } = useAuth();
  const [personel, setPersonel] = useState(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('info');

  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
  
  // Nereden geldiğini tespit et
  const fromPage = location.state?.from;
  const backLink = fromPage === '/personels' ? '/personels' : '/my-team';
  const backText = fromPage === '/personels' ? '← Tüm Personellere Dön' : '← Takımıma Dön';

  // Maaş görme yetkisi
  const canViewSalary = isAdmin || hasPermission('personel.view.salary');

  useEffect(() => {
    fetchPersonelDetail();
  }, [id]);

  const fetchPersonelDetail = async () => {
    try {
      setLoading(true);
      const response = await api.get(`/Personels/${id}`);
      setPersonel(response.data);
    } catch (error) {
      console.error('Hata:', error);
      toast.error('Personel bilgileri yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const formatSalary = (salary, currency) => {
    if (!salary) return '-';
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency || 'TRY',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(salary);
  };


  const personelNumberCard = { label: 'Personel No', value: personel?.personnelNumber || '-', icon: '🔢' };
  const registrationNumberCard = { label: 'Sicil No', value: personel?.registrationNumber || '-', icon: '📋' };

  const infoCards = [
    { label: 'Ad Soyad', value: `${personel?.firstName || '-'} ${personel?.lastName || '-'}`, icon: '👤' },
    { label: 'Email', value: personel?.email || '-', icon: '📧' },
    { label: 'Telefon', value: personel?.phone || '-', icon: '📞' },
    { label: 'Departman', value: personel?.departmentName || '-', icon: '🏢' },
    { label: 'Pozisyon', value: personel?.positionName || '-', icon: '💼' },
    { label: 'Yönetici', value: personel?.managerName || '-', icon: '👔' },
    { label: 'Personel No', value: personel?.personnelNumber || '-', icon: '🔢' },      
    { label: 'Sicil No', value: personel?.registrationNumber || '-', icon: '📋' },      
    { label: 'Kayıt Tarihi', value: personel?.createdAt ? new Date(personel.createdAt).toLocaleDateString('tr-TR') : '-', icon: '📅' },
    { label: 'Durum', value: personel?.isActive ? '✅ Aktif' : '❌ Pasif', icon: '📊' }
  ];

  if (loading) {
    return (
      <div className="flex justify-center items-center h-96">
        <div className="relative">
          <div className="w-16 h-16 border-4 border-blue-200 border-t-blue-600 rounded-full animate-spin"></div>
        </div>
      </div>
    );
  }

  if (!personel) {
    return (
      <div className="text-center py-16">
        <div className="text-6xl mb-4">🔍</div>
        <p className="text-gray-500 dark:text-gray-400">Personel bulunamadı</p>
        <Link to={backLink} className="inline-flex items-center gap-2 mt-4 text-blue-600 hover:text-blue-700">
          ← Geri Dön
        </Link>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800">
      <div className="max-w-6xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Link 
            to={backLink}
            className="inline-flex items-center gap-2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 mb-4 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {backText}
          </Link>
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Personel Detayı</h1>
              <p className="text-gray-500 dark:text-gray-400 mt-1">Personel bilgilerini görüntüleyin</p>
            </div>
          </div>
        </div>

        {/* Hero Card */}
        <div className="relative bg-gradient-to-r from-blue-600 to-purple-600 rounded-2xl overflow-hidden shadow-xl mb-8">
          <div className="absolute inset-0 bg-black/10"></div>
          <div className="relative p-8 flex flex-col md:flex-row items-center gap-6">
            <div className="w-24 h-24 bg-white/20 rounded-2xl backdrop-blur-sm flex items-center justify-center text-4xl font-bold text-white shadow-lg">
              {personel.firstName?.charAt(0)}{personel.lastName?.charAt(0)}
            </div>
            <div className="flex-1 text-center md:text-left">
              <h2 className="text-2xl md:text-3xl font-bold text-white mb-2">
                {personel.firstName} {personel.lastName}
              </h2>
              <p className="text-blue-100 mb-3">{personel.email}</p>
              <div className="flex flex-wrap gap-2 justify-center md:justify-start">
                <span className="px-3 py-1 bg-white/20 rounded-full text-sm text-white">
                  {personel.positionName || 'Pozisyon belirtilmemiş'}
                </span>
                <span className="px-3 py-1 bg-white/20 rounded-full text-sm text-white">
                  {personel.departmentName || 'Departman belirtilmemiş'}
                </span>
              </div>
            </div>
            <div>
              {personel.isActive ? (
                <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium bg-emerald-500/20 text-emerald-100">
                  ✅ Aktif
                </span>
              ) : (
                <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium bg-red-500/20 text-red-100">
                  ❌ Pasif
                </span>
              )}
            </div>
          </div>
        </div>

        {/* Tab Menu */}
        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg overflow-hidden">
          <div className="border-b border-gray-200 dark:border-gray-700">
            <div className="flex overflow-x-auto px-4 gap-1">
              <button
                onClick={() => setActiveTab('info')}
                className={`flex items-center gap-2 px-5 py-3 text-sm font-medium transition-all duration-200 border-b-2 ${
                  activeTab === 'info'
                    ? 'text-blue-600 border-blue-600'
                    : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 border-transparent'
                }`}
              >
                <span className="text-lg">👤</span>
                <span>Kişisel Bilgiler</span>
              </button>
              <button
                onClick={() => setActiveTab('work')}
                className={`flex items-center gap-2 px-5 py-3 text-sm font-medium transition-all duration-200 border-b-2 ${
                  activeTab === 'work'
                    ? 'text-blue-600 border-blue-600'
                    : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 border-transparent'
                }`}
              >
                <span className="text-lg">💼</span>
                <span>İş Bilgileri</span>
              </button>
              <button
                onClick={() => setActiveTab('address')}
                className={`flex items-center gap-2 px-5 py-3 text-sm font-medium transition-all duration-200 border-b-2 ${
                  activeTab === 'address'
                    ? 'text-blue-600 border-blue-600'
                    : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 border-transparent'
                }`}
              >
                <span className="text-lg">📍</span>
                <span>Adres Bilgileri</span>
              </button>
            </div>
          </div>

          <div className="p-6">
            {/* Kişisel Bilgiler Tabı */}
            {activeTab === 'info' && (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {infoCards.map((item, index) => (
                  <div key={index} className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-4 hover:shadow-md transition-shadow">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-blue-100 dark:bg-blue-900/50 rounded-lg flex items-center justify-center text-xl">
                        {item.icon}
                      </div>
                      <div className="flex-1">
                        <p className="text-xs text-gray-500 dark:text-gray-400 uppercase tracking-wide">{item.label}</p>
                        <p className="text-sm font-medium text-gray-900 dark:text-white mt-0.5 break-all">
                          {item.value}
                        </p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {/* İş Bilgileri Tabı */}
            {activeTab === 'work' && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-4">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-amber-100 dark:bg-amber-900/50 rounded-lg flex items-center justify-center text-xl">💰</div>
                    <div>
                      <p className="text-xs text-gray-500 dark:text-gray-400">Maaş</p>
                      <p className="text-sm font-medium text-gray-900 dark:text-white mt-0.5">
                        {canViewSalary ? formatSalary(personel.salary, personel.currency) : '🔒 Yetkiniz yok'}
                      </p>
                    </div>
                  </div>
                </div>
                <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-4">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-green-100 dark:bg-green-900/50 rounded-lg flex items-center justify-center text-xl">📅</div>
                    <div>
                      <p className="text-xs text-gray-500 dark:text-gray-400">İşe Başlama</p>
                      <p className="text-sm font-medium text-gray-900 dark:text-white">
                        {personel.hireDate ? new Date(personel.hireDate).toLocaleDateString('tr-TR') : '-'}
                      </p>
                    </div>
                  </div>
                </div>
                <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-4">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-purple-100 dark:bg-purple-900/50 rounded-lg flex items-center justify-center text-xl">👑</div>
                    <div>
                      <p className="text-xs text-gray-500 dark:text-gray-400">Kullanıcı Hesabı</p>
                      <p className="text-sm font-medium text-gray-900 dark:text-white">
                        {personel.userId ? (
                          <span className="text-emerald-600 dark:text-emerald-400">✓ Aktif</span>
                        ) : (
                          <span className="text-red-600 dark:text-red-400">✗ Yok</span>
                        )}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Adres Bilgileri Tabı */}
            {activeTab === 'address' && (
              <div className="space-y-4">
                <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-10 h-10 bg-indigo-100 dark:bg-indigo-900/50 rounded-lg flex items-center justify-center text-xl shrink-0">📍</div>
                    <div className="flex-1">
                      <p className="text-xs text-gray-500 dark:text-gray-400">Adres</p>
                      <p className="text-sm font-medium text-gray-900 dark:text-white">{personel.address || '-'}</p>
                    </div>
                  </div>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-4">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-cyan-100 dark:bg-cyan-900/50 rounded-lg flex items-center justify-center text-xl">🏙️</div>
                      <div>
                        <p className="text-xs text-gray-500 dark:text-gray-400">Şehir</p>
                        <p className="text-sm font-medium text-gray-900 dark:text-white">{personel.city || '-'}</p>
                      </div>
                    </div>
                  </div>
                  <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-4">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-orange-100 dark:bg-orange-900/50 rounded-lg flex items-center justify-center text-xl">🏘️</div>
                      <div>
                        <p className="text-xs text-gray-500 dark:text-gray-400">İlçe</p>
                        <p className="text-sm font-medium text-gray-900 dark:text-white">{personel.district || '-'}</p>
                      </div>
                    </div>
                  </div>
                  <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-4">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-pink-100 dark:bg-pink-900/50 rounded-lg flex items-center justify-center text-xl">📮</div>
                      <div>
                        <p className="text-xs text-gray-500 dark:text-gray-400">Posta Kodu</p>
                        <p className="text-sm font-medium text-gray-900 dark:text-white">{personel.postalCode || '-'}</p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}