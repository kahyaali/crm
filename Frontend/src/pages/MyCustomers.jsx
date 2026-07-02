import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import api from '../services/api';

export default function MyCustomers() {
  const { user } = useAuth();
  const [customers, setCustomers] = useState([]);
  const [loading, setLoading] = useState(true);
  
  // Pagination
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [paymentTypeFilter, setPaymentTypeFilter] = useState('');
  const pageSize = 10;

  useEffect(() => {
    fetchMyCustomers();
  }, [page, search, statusFilter, paymentTypeFilter]);

  const fetchMyCustomers = async () => {
    try {
      setLoading(true);
      const response = await api.get('/Customers/my-customers', {
        params: { page, pageSize, search, status: statusFilter, paymentType: paymentTypeFilter }
      });
      setCustomers(response.data.customers || []);
      setTotalCount(response.data.totalCount || 0);
      setTotalPages(response.data.totalPages || 1);
    } catch (error) {
      console.error('Müşteriler yüklenemedi:', error);
      toast.error('Müşteri listesi yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => {
    setSearch(searchInput);
    setPage(1);
  };

  const handleClearFilters = () => {
    setSearchInput('');
    setSearch('');
    setStatusFilter('');
    setPaymentTypeFilter('');
    setPage(1);
  };

  const getStatusBadge = (status) => {
    const statusConfig = {
      'Active': { color: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200', label: '✅ Aktif' },
      'Passive': { color: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200', label: '❌ Pasif' },
      'Pending': { color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200', label: '⏳ Beklemede' },
      'Lead': { color: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200', label: '🎯 Potansiyel' },
      'Lost': { color: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300', label: '💔 Kaybedilen' }
    };
    const config = statusConfig[status] || statusConfig['Pending'];
    return <span className={`px-2 py-1 rounded-full text-xs ${config.color}`}>{config.label}</span>;
  };

  const getPaymentTypeBadge = (type) => {
    if (!type) return <span className="text-gray-400">-</span>;
    const config = {
      'Cash': { color: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-200', label: '💵 Peşin' },
      'Credit': { color: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200', label: '💳 Kredili' },
      'Deferred': { color: 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200', label: '📅 Vadeli' }
    };
    const c = config[type];
    return <span className={`px-2 py-1 rounded-full text-xs ${c.color}`}>{c.label}</span>;
  };

  // 🔥 Aktif/Pasif Badge
  const getActiveBadge = (isActive) => {
    return isActive ? (
      <span className="px-2 py-1 rounded-full text-xs bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">
        ✅ Aktif
      </span>
    ) : (
      <span className="px-2 py-1 rounded-full text-xs bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200">
        ❌ Pasif
      </span>
    );
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 dark:border-blue-400"></div>
      </div>
    );
  }

  return (
    <div className="p-6">
      {/* Başlık */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Müşterilerim</h1>
        <p className="text-gray-500 dark:text-gray-400">Size atanmış müşteriler ({totalCount} müşteri)</p>
      </div>

      {/* Arama ve Filtre */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4 mb-6">
        <div className="flex flex-wrap gap-2">
          <input
            type="text"
            placeholder="Cari No, isim, soyisim, email, şirket ara..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            className="flex-1 min-w-[200px] p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400"
          />
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white"
          >
            <option value="">Tüm Durumlar</option>
            <option value="Active">✅ Aktif</option>
            <option value="Passive">❌ Pasif</option>
            <option value="Pending">⏳ Beklemede</option>
            <option value="Lead">🎯 Potansiyel</option>
            <option value="Lost">💔 Kaybedilen</option>
          </select>
          <select
            value={paymentTypeFilter}
            onChange={(e) => setPaymentTypeFilter(e.target.value)}
            className="p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white"
          >
            <option value="">Tüm Ödemeler</option>
            <option value="Cash">💵 Peşin</option>
            <option value="Credit">💳 Kredili</option>
            <option value="Deferred">📅 Vadeli</option>
          </select>
          <button onClick={handleSearch} className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors">
            Ara
          </button>
          <button onClick={handleClearFilters} className="bg-gray-500 text-white px-4 py-2 rounded-lg hover:bg-gray-600 transition-colors">
            Temizle
          </button>
        </div>
      </div>

      {/* Tablo */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-700">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Cari No</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Müşteri</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Email</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Telefon</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Şirket</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Ödeme</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Durum</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Aktif/Pasif</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Kayıt Tarihi</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">İşlem</th>
              </tr>
            </thead>
            <tbody>
              {customers.length === 0 ? (
                <tr>
                  <td colSpan={10} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                    Size atanmış müşteri bulunmuyor
                  </td>
                </tr>
              ) : (
                customers.map((customer) => (
                  <tr key={customer.id} className="border-t border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                    <td className="px-6 py-4 font-mono text-sm font-medium text-blue-600 dark:text-blue-400">
                      {customer.accountNumber || '-'}
                    </td>
                    <td className="px-6 py-4 font-medium text-gray-800 dark:text-white">
                      {customer.firstName} {customer.lastName}
                    </td>
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300 text-sm">{customer.email}</td>
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300 text-sm">{customer.phone || '-'}</td>
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300 text-sm">{customer.companyName || '-'}</td>
                    <td className="px-6 py-4">{getPaymentTypeBadge(customer.paymentType)}</td>
                    <td className="px-6 py-4">{getStatusBadge(customer.status)}</td>
                    <td className="px-6 py-4">{getActiveBadge(customer.isActive)}</td>
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300 text-sm">
                      {customer.createdAt?.split('T')[0]}
                    </td>
                    <td className="px-6 py-4 text-center">
                      <Link
                        to={`/customer-detail/${customer.id}`}
                        state={{ from: '/my-customers' }}
                        className="inline-flex items-center gap-1 px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white text-xs font-medium rounded-lg transition-colors"
                      >
                        👁️ Detay
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex justify-between items-center mt-4">
          <div className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} müşteri</div>
          <div className="flex gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="px-3 py-1 border rounded disabled:opacity-50 hover:bg-gray-100 dark:border-gray-600 dark:text-white dark:hover:bg-gray-700 transition-colors"
            >
              ◀
            </button>
            <span className="px-3 py-1 text-sm text-gray-700 dark:text-gray-300">Sayfa {page} / {totalPages}</span>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="px-3 py-1 border rounded disabled:opacity-50 hover:bg-gray-100 dark:border-gray-600 dark:text-white dark:hover:bg-gray-700 transition-colors"
            >
              ▶
            </button>
          </div>
        </div>
      )}
    </div>
  );
}