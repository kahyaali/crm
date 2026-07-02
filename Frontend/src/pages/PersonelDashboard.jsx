import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import toast from 'react-hot-toast';
import api from '../services/api';

export default function PersonelDashboard() {
  const { user } = useAuth();
  const [myInfo, setMyInfo] = useState(null);
  const [teamMembers, setTeamMembers] = useState([]);
  const [myCustomers, setMyCustomers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('team');
  
  // Takım pagination
  const [teamPage, setTeamPage] = useState(1);
  const [teamTotalPages, setTeamTotalPages] = useState(1);
  const [teamTotalCount, setTeamTotalCount] = useState(0);
  const [teamSearch, setTeamSearch] = useState('');
  const [teamSearchInput, setTeamSearchInput] = useState('');
  
  // Müşteri pagination
  const [customerPage, setCustomerPage] = useState(1);
  const [customerTotalPages, setCustomerTotalPages] = useState(1);
  const [customerTotalCount, setCustomerTotalCount] = useState(0);
  const [customerSearch, setCustomerSearch] = useState('');
  const [customerSearchInput, setCustomerSearchInput] = useState('');
  const [customerStatus, setCustomerStatus] = useState('');

  // Manager mı kontrol et (maaş göstermek için)
  const isManager = user?.role === 'SystemAdmin' || user?.role === 'Admin' || user?.role === 'SatisMuduru';

  // Sayfa ilk yüklendiğinde her iki veriyi de çek
  useEffect(() => {
    fetchMyTeam();
    fetchMyCustomers();
  }, []);

  // Takım sayfa veya arama değişince tekrar yükle
  useEffect(() => {
    if (teamPage !== 1 || teamSearch !== '') {
      fetchMyTeam();
    }
  }, [teamPage, teamSearch]);

  // Müşteri sayfa, arama veya filtre değişince tekrar yükle
  useEffect(() => {
    if (customerPage !== 1 || customerSearch !== '' || customerStatus !== '') {
      fetchMyCustomers();
    }
  }, [customerPage, customerSearch, customerStatus]);

  const fetchMyTeam = async () => {
    try {
      const response = await api.get('/Personels/my-team', {
        params: { page: teamPage, pageSize: 10, search: teamSearch }
      });
      console.log('Takım response:', response.data);
      setMyInfo(response.data.currentPersonel);
      setTeamMembers(response.data.teamMembers || []);
      setTeamTotalCount(response.data.totalCount || 0);
      setTeamTotalPages(response.data.totalPages || 1);
    } catch (error) {
      console.error('Takım yüklenemedi:', error);
      toast.error('Takım bilgileri yüklenemedi');
    }
  };

  const fetchMyCustomers = async () => {
    try {
      const response = await api.get('/Customers/my-customers', {
        params: { 
          page: customerPage, 
          pageSize: 10, 
          search: customerSearch,
          status: customerStatus 
        }
      });
      console.log('Müşteri response:', response.data);
      setMyCustomers(response.data.customers || []);
      setCustomerTotalCount(response.data.totalCount || 0);
      setCustomerTotalPages(response.data.totalPages || 1);
    } catch (error) {
      console.error('Müşteriler yüklenemedi:', error);
      toast.error('Müşteri listesi yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  // Takım arama
  const handleTeamSearch = () => {
    setTeamSearch(teamSearchInput);
    setTeamPage(1);
  };

  // Müşteri arama
  const handleCustomerSearch = () => {
    setCustomerSearch(customerSearchInput);
    setCustomerPage(1);
  };

  // Sekme değişince sayfa numaralarını sıfırla
  const handleTabChange = (tab) => {
    setActiveTab(tab);
    if (tab === 'team') {
      setTeamPage(1);
    } else {
      setCustomerPage(1);
    }
  };

  // Maaşı formatla
  const formatSalary = (salary, currency) => {
    if (!salary) return '-';
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency || 'TRY',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(salary);
  };

  // Status badge
  const getStatusBadge = (status) => {
    const statusConfig = {
      'Active': { color: 'bg-green-100 text-green-800', label: '✅ Aktif' },
      'Passive': { color: 'bg-red-100 text-red-800', label: '❌ Pasif' },
      'Pending': { color: 'bg-yellow-100 text-yellow-800', label: '⏳ Beklemede' },
      'Lead': { color: 'bg-blue-100 text-blue-800', label: '🎯 Potansiyel' },
      'Lost': { color: 'bg-gray-100 text-gray-800', label: '💔 Kaybedilen' }
    };
    const config = statusConfig[status] || statusConfig['Pending'];
    return <span className={`px-2 py-1 rounded-full text-xs ${config.color}`}>{config.label}</span>;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="p-6">
      {/* Hoşgeldin Kartı */}
      <div className="bg-gradient-to-r from-blue-600 to-purple-600 rounded-2xl p-6 mb-6 text-white">
        <div className="flex items-center gap-4">
          {myInfo?.avatarUrl ? (
            <img src={myInfo.avatarUrl} className="w-16 h-16 rounded-full border-4 border-white" />
          ) : (
            <div className="w-16 h-16 bg-white/20 rounded-full flex items-center justify-center text-2xl">👤</div>
          )}
          <div>
            <h1 className="text-2xl font-bold mb-1">
              Hoşgeldin, {myInfo?.firstName} {myInfo?.lastName}
            </h1>
            <p className="opacity-90">{myInfo?.positionName || 'Personel'} | {myInfo?.departmentName || 'Departman'}</p>
            <p className="text-sm opacity-75 mt-1">{myInfo?.email}</p>
            {isManager && myInfo?.salary && (
              <p className="text-sm opacity-90 mt-1">
                Maaş: {formatSalary(myInfo.salary, myInfo.currency)}
              </p>
            )}
          </div>
        </div>
      </div>

      {/* İstatistikler */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
        <div className="bg-white dark:bg-gray-800 rounded-xl p-6 shadow-lg">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Takımım</p>
              <p className="text-3xl font-bold text-blue-600">{teamTotalCount}</p>
              <p className="text-sm text-gray-500">Bağlı personel</p>
            </div>
            <div className="w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center">
              <span className="text-2xl">👥</span>
            </div>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-xl p-6 shadow-lg">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-500 text-sm">Müşterilerim</p>
              <p className="text-3xl font-bold text-green-600">{customerTotalCount}</p>
              <p className="text-sm text-gray-500">Atanan müşteri</p>
            </div>
            <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center">
              <span className="text-2xl">👤</span>
            </div>
          </div>
        </div>
      </div>

      {/* Tab Menü */}
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg overflow-hidden">
        <div className="border-b border-gray-200 dark:border-gray-700">
          <div className="flex">
            <button
              onClick={() => handleTabChange('team')}
              className={`px-6 py-3 text-sm font-medium transition-colors ${
                activeTab === 'team'
                  ? 'text-blue-600 border-b-2 border-blue-600'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              👥 Takımım ({teamTotalCount})
            </button>
            <button
              onClick={() => handleTabChange('customers')}
              className={`px-6 py-3 text-sm font-medium transition-colors ${
                activeTab === 'customers'
                  ? 'text-blue-600 border-b-2 border-blue-600'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              📋 Müşterilerim ({customerTotalCount})
            </button>
          </div>
        </div>

        {/* Takımım Tablosu */}
        {activeTab === 'team' && (
          <div className="p-6">
            {/* Arama */}
            <div className="mb-4 flex gap-2">
              <input
                type="text"
                placeholder="Personel ara (isim, soyisim, email)..."
                value={teamSearchInput}
                onChange={(e) => setTeamSearchInput(e.target.value)}
                onKeyPress={(e) => e.key === 'Enter' && handleTeamSearch()}
                className="flex-1 p-2 border rounded-lg dark:bg-gray-700"
              />
              <button onClick={handleTeamSearch} className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700">
                Ara
              </button>
            </div>
            
            {teamMembers.length === 0 ? (
              <div className="text-center py-12 text-gray-500">
                <span className="text-6xl mb-4 block">👥</span>
                <p>Henüz bağlı personeliniz bulunmuyor</p>
              </div>
            ) : (
              <>
                <div className="overflow-x-auto">
                  <table className="min-w-full">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-3">Personel</th>
                        <th className="text-left py-3">Email</th>
                        <th className="text-left py-3">Pozisyon</th>
                        <th className="text-left py-3">Departman</th>
                        {isManager && <th className="text-left py-3">Maaş</th>}
                        <th className="text-left py-3">İşe Başlama</th>
                      </tr>
                    </thead>
                    <tbody>
                      {teamMembers.map((member) => (
                        <tr key={member.id} className="border-b hover:bg-gray-50">
                          <td className="py-3">
                            <div className="flex items-center gap-3">
                              {member.avatarUrl ? (
                                <img src={member.avatarUrl} className="w-8 h-8 rounded-full object-cover" />
                              ) : (
                                <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center text-white text-sm">
                                  {member.firstName?.charAt(0)}{member.lastName?.charAt(0)}
                                </div>
                              )}
                              <span>{member.firstName} {member.lastName}</span>
                            </div>
                          </td>
                          <td className="py-3">{member.email}</td>
                          <td className="py-3">{member.positionName || '-'}</td>
                          <td className="py-3">{member.departmentName || '-'}</td>
                          {isManager && (
                            <td className="py-3">{formatSalary(member.salary, member.currency)}</td>
                          )}
                          <td className="py-3">{member.hireDate?.split('T')[0] || '-'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                
                {/* Pagination */}
                {teamTotalPages > 1 && (
                  <div className="flex justify-between items-center mt-4">
                    <div className="text-sm text-gray-500">Toplam {teamTotalCount} personel</div>
                    <div className="flex gap-2">
                      <button
                        onClick={() => setTeamPage(p => Math.max(1, p - 1))}
                        disabled={teamPage === 1}
                        className="px-3 py-1 border rounded disabled:opacity-50 hover:bg-gray-100"
                      >
                        ◀ Önceki
                      </button>
                      <span className="px-3 py-1 text-sm">
                        Sayfa {teamPage} / {teamTotalPages}
                      </span>
                      <button
                        onClick={() => setTeamPage(p => Math.min(teamTotalPages, p + 1))}
                        disabled={teamPage === teamTotalPages}
                        className="px-3 py-1 border rounded disabled:opacity-50 hover:bg-gray-100"
                      >
                        Sonraki ▶
                      </button>
                    </div>
                  </div>
                )}
              </>
            )}
          </div>
        )}

        {/* Müşterilerim Tablosu */}
        {activeTab === 'customers' && (
          <div className="p-6">
            {/* Arama ve Filtre */}
            <div className="mb-4 flex flex-wrap gap-2">
              <input
                type="text"
                placeholder="Müşteri ara (isim, soyisim, email, şirket)..."
                value={customerSearchInput}
                onChange={(e) => setCustomerSearchInput(e.target.value)}
                onKeyPress={(e) => e.key === 'Enter' && handleCustomerSearch()}
                className="flex-1 p-2 border rounded-lg dark:bg-gray-700"
              />
              <select
                value={customerStatus}
                onChange={(e) => {
                  setCustomerStatus(e.target.value);
                  setCustomerPage(1);
                }}
                className="p-2 border rounded-lg dark:bg-gray-700"
              >
                <option value="">Tüm Durumlar</option>
                <option value="Active">✅ Aktif</option>
                <option value="Passive">❌ Pasif</option>
                <option value="Pending">⏳ Beklemede</option>
                <option value="Lead">🎯 Potansiyel</option>
                <option value="Lost">💔 Kaybedilen</option>
              </select>
              <button onClick={handleCustomerSearch} className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700">
                Ara
              </button>
            </div>
            
            {myCustomers.length === 0 ? (
              <div className="text-center py-12 text-gray-500">
                <span className="text-6xl mb-4 block">👤</span>
                <p>Henüz atanmış müşteriniz bulunmuyor</p>
              </div>
            ) : (
              <>
                <div className="overflow-x-auto">
                  <table className="min-w-full">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-3">Müşteri</th>
                        <th className="text-left py-3">Email</th>
                        <th className="text-left py-3">Telefon</th>
                        <th className="text-left py-3">Şirket</th>
                        <th className="text-left py-3">Durum</th>
                        <th className="text-left py-3">Kayıt Tarihi</th>
                      </tr>
                    </thead>
                    <tbody>
                      {myCustomers.map((customer) => (
                        <tr key={customer.id} className="border-b hover:bg-gray-50">
                          <td className="py-3">{customer.firstName} {customer.lastName}</td>
                          <td className="py-3">{customer.email}</td>
                          <td className="py-3">{customer.phone || '-'}</td>
                          <td className="py-3">{customer.companyName || '-'}</td>
                          <td className="py-3">{getStatusBadge(customer.status)}</td>
                          <td className="py-3">{customer.createdAt?.split('T')[0]}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                
                {/* Pagination */}
                {customerTotalPages > 1 && (
                  <div className="flex justify-between items-center mt-4">
                    <div className="text-sm text-gray-500">Toplam {customerTotalCount} müşteri</div>
                    <div className="flex gap-2">
                      <button
                        onClick={() => setCustomerPage(p => Math.max(1, p - 1))}
                        disabled={customerPage === 1}
                        className="px-3 py-1 border rounded disabled:opacity-50 hover:bg-gray-100"
                      >
                        ◀ Önceki
                      </button>
                      <span className="px-3 py-1 text-sm">
                        Sayfa {customerPage} / {customerTotalPages}
                      </span>
                      <button
                        onClick={() => setCustomerPage(p => Math.min(customerTotalPages, p + 1))}
                        disabled={customerPage === customerTotalPages}
                        className="px-3 py-1 border rounded disabled:opacity-50 hover:bg-gray-100"
                      >
                        Sonraki ▶
                      </button>
                    </div>
                  </div>
                )}
              </>
            )}
          </div>
        )}
      </div>
    </div>
  );
}