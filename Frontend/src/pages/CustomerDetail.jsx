import { useState, useEffect } from 'react';
import { useParams, Link, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import toast from 'react-hot-toast';
import api from '../services/api';
import Swal from 'sweetalert2';

export default function CustomerDetail() {
  const { id } = useParams();
  const location = useLocation();
  const { user } = useAuth();
  const [customer, setCustomer] = useState(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('personal');

  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';

  // Nereden geldiğini tespit et
  const fromPage = location.state?.from;
  const referrer = document.referrer;
  
  let backLink = '/customers';
  let backText = '← Müşterilere Dön';
  
  if (fromPage === '/my-customers') {
    backLink = '/my-customers';
    backText = '← Müşterilerime Dön';
  } else if (fromPage === '/customers') {
    backLink = '/customers';
    backText = '← Tüm Müşterilere Dön';
  } else if (referrer.includes('/my-customers')) {
    backLink = '/my-customers';
    backText = '← Müşterilerime Dön';
  } else if (referrer.includes('/customers')) {
    backLink = '/customers';
    backText = '← Müşterilere Dön';
  }

  useEffect(() => {
    fetchCustomerDetail();
  }, [id]);

  const fetchCustomerDetail = async () => {
    try {
      setLoading(true);
      const response = await api.get(`/Customers/${id}`);
      setCustomer(response.data);
    } catch (error) {
      console.error('Hata:', error);
      toast.error('Müşteri bilgileri yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  // 🔥 Aktif/Pasif değiştirme fonksiyonları
  const handleActivate = async () => {
    const result = await Swal.fire({
      title: 'Müşteriyi Aktif Yap',
      text: `${customer.firstName} ${customer.lastName} müşterisini aktif hale getirmek istediğinize emin misiniz?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Aktif Yap',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        await api.post(`/Customers/${id}/activate`);
        toast.success('Müşteri aktif hale getirildi');
        fetchCustomerDetail();
      } catch (error) {
        toast.error(error.response?.data?.message || 'İşlem başarısız');
      }
    }
  };

  const handleDeactivate = async () => {
    const result = await Swal.fire({
      title: 'Müşteriyi Pasif Yap',
      text: `${customer.firstName} ${customer.lastName} müşterisini pasif hale getirmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Pasif Yap',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        await api.post(`/Customers/${id}/deactivate`);
        toast.success('Müşteri pasif hale getirildi');
        fetchCustomerDetail();
      } catch (error) {
        toast.error(error.response?.data?.message || 'İşlem başarısız');
      }
    }
  };

  const getStatusBadge = (status) => {
    const config = {
      'Active': { icon: '✅', text: 'Aktif', color: 'emerald' },
      'Passive': { icon: '❌', text: 'Pasif', color: 'red' },
      'Pending': { icon: '⏳', text: 'Beklemede', color: 'amber' },
      'Lead': { icon: '🎯', text: 'Potansiyel', color: 'blue' },
      'Lost': { icon: '💔', text: 'Kaybedilen', color: 'gray' }
    };
    const c = config[status] || config['Pending'];
    const colorClasses = {
      emerald: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300',
      red: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300',
      amber: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300',
      blue: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
      gray: 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300'
    };
    return (
      <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium ${colorClasses[c.color]}`}>
        <span>{c.icon}</span> {c.text}
      </span>
    );
  };

  // 🔥 Aktif/Pasif Badge
  const getActiveBadge = (isActive) => {
    return isActive ? (
      <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300">
        <span>✅</span> Aktif
      </span>
    ) : (
      <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300">
        <span>❌</span> Pasif
      </span>
    );
  };

  const getPaymentTypeBadge = (type) => {
    if (!type) return <span className="text-gray-400">-</span>;
    const config = {
      'Cash': { icon: '💵', text: 'Peşin', color: 'emerald' },
      'Credit': { icon: '💳', text: 'Kredili', color: 'purple' },
      'Deferred': { icon: '📅', text: 'Vadeli', color: 'amber' }
    };
    const c = config[type];
    const colorClasses = {
      emerald: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30',
      purple: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30',
      amber: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30'
    };
    return (
      <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium ${colorClasses[c.color]}`}>
        {c.icon} {c.text}
      </span>
    );
  };

  const InfoCard = ({ icon, title, value, color = 'blue' }) => {
    const colorMap = {
      blue: 'from-blue-50 to-white dark:from-blue-900/20 dark:to-gray-800 border-blue-100 dark:border-blue-800/50 bg-blue-100 dark:bg-blue-900/50',
      indigo: 'from-indigo-50 to-white dark:from-indigo-900/20 dark:to-gray-800 border-indigo-100 dark:border-indigo-800/50 bg-indigo-100 dark:bg-indigo-900/50',
      green: 'from-green-50 to-white dark:from-green-900/20 dark:to-gray-800 border-green-100 dark:border-green-800/50 bg-green-100 dark:bg-green-900/50',
      purple: 'from-purple-50 to-white dark:from-purple-900/20 dark:to-gray-800 border-purple-100 dark:border-purple-800/50 bg-purple-100 dark:bg-purple-900/50',
      amber: 'from-amber-50 to-white dark:from-amber-900/20 dark:to-gray-800 border-amber-100 dark:border-amber-800/50 bg-amber-100 dark:bg-amber-900/50',
      cyan: 'from-cyan-50 to-white dark:from-cyan-900/20 dark:to-gray-800 border-cyan-100 dark:border-cyan-800/50 bg-cyan-100 dark:bg-cyan-900/50',
      pink: 'from-pink-50 to-white dark:from-pink-900/20 dark:to-gray-800 border-pink-100 dark:border-pink-800/50 bg-pink-100 dark:bg-pink-900/50',
      gray: 'from-gray-50 to-white dark:from-gray-800/50 dark:to-gray-800 border-gray-100 dark:border-gray-700 bg-gray-100 dark:bg-gray-700/50',
      emerald: 'from-emerald-50 to-white dark:from-emerald-900/20 dark:to-gray-800 border-emerald-100 dark:border-emerald-800/50 bg-emerald-100 dark:bg-emerald-900/50',
      orange: 'from-orange-50 to-white dark:from-orange-900/20 dark:to-gray-800 border-orange-100 dark:border-orange-800/50 bg-orange-100 dark:bg-orange-900/50'
    };
    return (
      <div className={`bg-gradient-to-br ${colorMap[color] || colorMap.blue} rounded-xl p-4 border hover:shadow-md transition-all`}>
        <div className="flex items-center gap-3">
          <div className={`w-10 h-10 rounded-lg flex items-center justify-center text-xl ${colorMap[color]?.split(' ')[4] || 'bg-blue-100 dark:bg-blue-900/50'}`}>{icon}</div>
          <div className="flex-1">
            <p className="text-xs text-gray-500 dark:text-gray-400 uppercase tracking-wide">{title}</p>
            <p className="text-sm font-semibold text-gray-900 dark:text-white mt-0.5 break-words">{value || '-'}</p>
          </div>
        </div>
      </div>
    );
  };

  const SectionTitle = ({ icon, title }) => (
    <div className="flex items-center gap-2 mb-4 pb-2 border-b border-gray-200 dark:border-gray-700">
      <span className="text-xl">{icon}</span>
      <h3 className="text-lg font-semibold text-gray-800 dark:text-white">{title}</h3>
    </div>
  );

  if (loading) {
    return (
      <div className="flex justify-center items-center h-96">
        <div className="relative">
          <div className="w-16 h-16 border-4 border-blue-200 border-t-blue-600 rounded-full animate-spin"></div>
          <p className="text-sm text-gray-500 mt-3">Yükleniyor...</p>
        </div>
      </div>
    );
  }

  if (!customer) {
    return (
      <div className="text-center py-16">
        <div className="text-6xl mb-4">🔍</div>
        <p className="text-gray-500 dark:text-gray-400">Müşteri bulunamadı</p>
        <Link to={backLink} className="inline-flex items-center gap-2 mt-4 text-blue-600 hover:text-blue-700 font-medium">
          ← Geri Dön
        </Link>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 via-white to-gray-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900">
      <div className="max-w-7xl mx-auto px-4 py-8">
        
        {/* Header - Geri Butonu */}
        <div className="mb-6">
          <Link 
            to={backLink} 
            state={{ from: location.pathname }}
            className="inline-flex items-center gap-2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 mb-4 transition-colors group"
          >
            <svg className="w-5 h-5 group-hover:-translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {backText}
          </Link>
        </div>

        {/* Hero Section */}
        <div className="relative bg-gradient-to-r from-blue-600 via-indigo-600 to-purple-600 rounded-2xl overflow-hidden shadow-2xl mb-8">
          <div className="absolute inset-0 bg-black/20"></div>
          <div className="absolute top-0 right-0 w-64 h-64 bg-white/10 rounded-full -translate-y-1/2 translate-x-1/3"></div>
          <div className="absolute bottom-0 left-0 w-48 h-48 bg-white/10 rounded-full translate-y-1/2 -translate-x-1/3"></div>
          <div className="relative p-8 flex flex-col md:flex-row items-center gap-6">
            <div className="w-28 h-28 bg-gradient-to-br from-white/30 to-white/10 rounded-2xl backdrop-blur-sm flex items-center justify-center text-5xl font-bold text-white shadow-xl border border-white/20">
              {customer.firstName?.charAt(0)}{customer.lastName?.charAt(0)}
            </div>
            <div className="flex-1 text-center md:text-left">
              <div className="flex flex-wrap items-center gap-3 justify-center md:justify-start mb-2">
                <h1 className="text-3xl md:text-4xl font-bold text-white">
                  {customer.firstName} {customer.lastName}
                </h1>
                {getActiveBadge(customer.isActive)}
                {getStatusBadge(customer.status)}
              </div>
              <p className="text-blue-100 mb-2 flex items-center gap-2 justify-center md:justify-start">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                {customer.email}
              </p>
              <div className="flex flex-wrap gap-2 justify-center md:justify-start">
                <span className="px-3 py-1 bg-white/20 rounded-full text-sm text-white backdrop-blur-sm font-mono">
                  #{customer.accountNumber || 'Cari No Yok'}
                </span>
                <span className="px-3 py-1 bg-white/20 rounded-full text-sm text-white backdrop-blur-sm">
                  {customer.customerType || 'Müşteri'}
                </span>
                {/* 🔥 AKTİF/PASİF DEĞİŞTİRME BUTONLARI */}
                {isAdmin && (
                  customer.isActive ? (
                    <button
                      onClick={handleDeactivate}
                      className="px-3 py-1 bg-amber-500/80 hover:bg-amber-600 rounded-full text-sm text-white backdrop-blur-sm transition-colors"
                    >
                      🔒 Pasif Yap
                    </button>
                  ) : (
                    <button
                      onClick={handleActivate}
                      className="px-3 py-1 bg-green-500/80 hover:bg-green-600 rounded-full text-sm text-white backdrop-blur-sm transition-colors"
                    >
                      🔓 Aktif Yap
                    </button>
                  )
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Tab Menu - Aynı */}
        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg overflow-hidden border border-gray-100 dark:border-gray-700">
          <div className="border-b border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50">
            <div className="flex overflow-x-auto px-4 gap-1">
              {[
                { id: 'personal', icon: '👤', label: 'Kişisel Bilgiler' },
                { id: 'corporate', icon: '🏢', label: 'Kurumsal Bilgiler' },
                { id: 'financial', icon: '💰', label: 'Finans Bilgileri' },
                { id: 'address', icon: '📍', label: 'Adres Bilgileri' },
                { id: 'contact', icon: '📞', label: 'İletişim' },
                { id: 'notes', icon: '📝', label: 'Notlar' }
              ].map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`flex items-center gap-2 px-5 py-3 text-sm font-medium transition-all duration-200 border-b-2 whitespace-nowrap ${
                    activeTab === tab.id
                      ? 'text-blue-600 border-blue-600 bg-blue-50/50 dark:bg-blue-900/20'
                      : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 border-transparent hover:border-gray-300'
                  }`}
                >
                  <span className="text-lg">{tab.icon}</span>
                  <span className="hidden sm:inline">{tab.label}</span>
                </button>
              ))}
            </div>
          </div>

          <div className="p-6">
            
            {/* TAB 1: KİŞİSEL BİLGİLER */}
            {activeTab === 'personal' && (
              <div className="space-y-6">
                <SectionTitle icon="👤" title="Kişisel Bilgiler" />
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                  <InfoCard icon="👤" title="Ad Soyad" value={`${customer.firstName} ${customer.lastName}`} color="blue" />
                  <InfoCard icon="📧" title="Email" value={customer.email} color="indigo" />
                  <InfoCard icon="📞" title="Telefon" value={customer.phone} color="green" />
                  <InfoCard icon="🏷️" title="Müşteri Tipi" value={customer.customerType} color="purple" />
                  <InfoCard icon="📊" title="Durum" value={getStatusBadge(customer.status)} color="amber" />
                  <InfoCard icon="🔘" title="Aktiflik" value={getActiveBadge(customer.isActive)} color="emerald" />
                  <InfoCard icon="👨‍💼" title="Sorumlu Personel" value={customer.assignedToPersonelName} color="cyan" />
                  <InfoCard icon="🆔" title="Cari Hesap No" value={customer.accountNumber} color="pink" />
                  <InfoCard icon="📅" title="Kayıt Tarihi" value={customer.createdAt ? new Date(customer.createdAt).toLocaleDateString('tr-TR') : '-'} color="gray" />
                </div>
              </div>
            )}

            {/* TAB 2: KURUMSAL BİLGİLER - Aynı */}
            {activeTab === 'corporate' && (
              <div className="space-y-6">
                <SectionTitle icon="🏢" title="Kurumsal Bilgiler" />
                {customer.customerType === 'Kurumsal' ? (
                  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                    <InfoCard icon="🏢" title="Şirket Adı" value={customer.companyName} color="purple" />
                    <InfoCard icon="📊" title="Vergi Numarası" value={customer.taxNumber} color="amber" />
                    <InfoCard icon="🏛️" title="Vergi Dairesi" value={customer.taxOffice} color="orange" />
                    <InfoCard icon="📋" title="Vergi İdaresi" value={customer.taxAdministration} color="indigo" />
                    <InfoCard icon="🌐" title="Web Sitesi" value={customer.website} color="blue" />
                    <InfoCard icon="👔" title="İlgili Kişi" value={customer.contactPerson} color="cyan" />
                    <InfoCard icon="📞" title="İlgili Kişi Telefon" value={customer.contactPersonPhone} color="green" />
                  </div>
                ) : (
                  <div className="text-center py-12 bg-gray-50 dark:bg-gray-700/30 rounded-xl">
                    <div className="text-5xl mb-3">🏢</div>
                    <p className="text-gray-500 dark:text-gray-400">Bu müşteri {customer.customerType || 'bireysel'} tiptedir.</p>
                    <p className="text-sm text-gray-400 mt-1">Kurumsal bilgiler sadece kurumsal müşteriler için görüntülenir.</p>
                  </div>
                )}
              </div>
            )}

            {/* TAB 3: FİNANS BİLGİLERİ - Aynı */}
            {activeTab === 'financial' && (
              <div className="space-y-6">
                <SectionTitle icon="💰" title="Finans Bilgileri" />
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                  <InfoCard icon="💳" title="Ödeme Tipi" value={getPaymentTypeBadge(customer.paymentType)} color="emerald" />
                  <InfoCard icon="🏦" title="Kredi Limiti" value={customer.creditLimit ? `₺ ${customer.creditLimit.toLocaleString('tr-TR')}` : '-'} color="blue" />
                  <InfoCard icon="📆" title="Vade Gün Sayısı" value={customer.paymentTermDays ? `${customer.paymentTermDays} gün` : '-'} color="purple" />
                  <InfoCard icon="🏷️" title="İndirim Oranı" value={customer.discountRate ? `% ${customer.discountRate}` : '-'} color="amber" />
                </div>
              </div>
            )}

            {/* TAB 4: ADRES BİLGİLERİ - Aynı */}
            {activeTab === 'address' && (
              <div className="space-y-6">
                <SectionTitle icon="📍" title="Adres Bilgileri" />
                <div className="grid grid-cols-1 gap-4">
                  <InfoCard icon="🏠" title="Adres" value={customer.address} color="gray" />
                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                    <InfoCard icon="🏙️" title="Şehir" value={customer.city} color="blue" />
                    <InfoCard icon="🏘️" title="İlçe" value={customer.district} color="cyan" />
                    <InfoCard icon="📮" title="Posta Kodu" value={customer.postalCode} color="purple" />
                  </div>
                  {(customer.shippingAddress || customer.invoiceAddress) && (
                    <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {customer.shippingAddress && (
                          <InfoCard icon="🚚" title="Teslimat Adresi" value={customer.shippingAddress} color="amber" />
                        )}
                        {customer.invoiceAddress && (
                          <InfoCard icon="📄" title="Fatura Adresi" value={customer.invoiceAddress} color="indigo" />
                        )}
                      </div>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* TAB 5: İLETİŞİM - Aynı */}
            {activeTab === 'contact' && (
              <div className="space-y-6">
                <SectionTitle icon="📞" title="İletişim Bilgileri" />
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <InfoCard icon="👔" title="İlgili Kişi" value={customer.contactPerson} color="blue" />
                  <InfoCard icon="📱" title="İlgili Kişi Telefon" value={customer.contactPersonPhone} color="green" />
                  <InfoCard icon="🌐" title="Web Sitesi" value={customer.website} color="purple" />
                  <InfoCard icon="📧" title="Email" value={customer.email} color="indigo" />
                  <InfoCard icon="📞" title="Telefon" value={customer.phone} color="emerald" />
                </div>
              </div>
            )}

            {/* TAB 6: NOTLAR - Aynı */}
            {activeTab === 'notes' && (
              <div className="space-y-6">
                <SectionTitle icon="📝" title="Müşteri Notları" />
                <div className="bg-gradient-to-br from-yellow-50 to-amber-50 dark:from-yellow-900/10 dark:to-amber-900/10 rounded-xl p-6 border border-yellow-200 dark:border-yellow-800/30">
                  <div className="flex items-start gap-3">
                    <div className="w-10 h-10 bg-yellow-100 dark:bg-yellow-900/50 rounded-lg flex items-center justify-center text-xl shrink-0">📝</div>
                    <div className="flex-1">
                      <p className="text-sm text-gray-700 dark:text-gray-300 whitespace-pre-wrap leading-relaxed">
                        {customer.notes || 'Henüz not eklenmemiş.'}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Footer Bilgi */}
        <div className="mt-6 text-center">
          <p className="text-xs text-gray-400 dark:text-gray-500">
            Son güncelleme: {customer.updatedAt ? new Date(customer.updatedAt).toLocaleString('tr-TR') : new Date(customer.createdAt).toLocaleString('tr-TR')}
          </p>
        </div>
      </div>
    </div>
  );
}