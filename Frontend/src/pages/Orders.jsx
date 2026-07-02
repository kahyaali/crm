import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';

export default function Orders() {
  const { user } = useAuth();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  
  // Pagination
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 10;
  
  // Filters
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [paymentStatusFilter, setPaymentStatusFilter] = useState('');
  const [customerIdFilter, setCustomerIdFilter] = useState('');
  const [customers, setCustomers] = useState([]);
  
  // Status lists
  const [statusOptions, setStatusOptions] = useState([]);
  const [paymentStatusOptions, setPaymentStatusOptions] = useState([]);

  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';

  useEffect(() => {
    fetchOrders();
    fetchCustomers();
    fetchStatusLists();
  }, [page, search, statusFilter, paymentStatusFilter, customerIdFilter]);

  const fetchOrders = async () => {
    try {
      setLoading(true);
      const response = await api.get('/Orders', {
        params: {
          page,
          pageSize,
          search,
          status: statusFilter,
          paymentStatus: paymentStatusFilter,
          customerId: customerIdFilter
        }
      });

      setOrders(response.data.data || []);
      setTotalPages(response.data.totalPages || 1);
      setTotalCount(response.data.totalCount || 0);
    } catch (error) {
      toast.error('Siparişler yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const fetchCustomers = async () => {
    try {
      const response = await api.get('/Orders/customer-list');
      setCustomers(response.data || []);
    } catch (error) {
      console.error('Müşteriler yüklenemedi:', error);
    }
  };

  const fetchStatusLists = async () => {
    try {
      const [statusRes, paymentRes] = await Promise.all([
        api.get('/Orders/status-list'),
        api.get('/Orders/payment-status-list')
      ]);
      setStatusOptions(statusRes.data || []);
      setPaymentStatusOptions(paymentRes.data || []);
    } catch (error) {
      console.error('Status listeleri yüklenemedi:', error);
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
    setPaymentStatusFilter('');
    setCustomerIdFilter('');
    setPage(1);
  };

  const handleDelete = async (id, orderNumber) => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${orderNumber} numaralı siparişi silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal',
      background: document.documentElement.classList.contains('dark') ? '#1f2937' : '#fff',
      color: document.documentElement.classList.contains('dark') ? '#f3f4f6' : '#1f2937'
    });

    if (result.isConfirmed) {
      try {
        await api.delete(`/Orders/${id}`);
        toast.success('Sipariş başarıyla silindi');
        fetchOrders();
      } catch (error) {
        toast.error(error.response?.data?.message || 'Silme hatası');
      }
    }
  };

  const getStatusBadge = (status) => {
    const config = {
      'Pending': { icon: '⏳', text: 'Beklemede', color: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300' },
      'Approved': { icon: '✅', text: 'Onaylandı', color: 'bg-sky-100 text-sky-800 dark:bg-sky-900/40 dark:text-sky-300' },
      'Preparing': { icon: '📦', text: 'Hazırlanıyor', color: 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-300' },
      'Shipped': { icon: '🚚', text: 'Kargolandı', color: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/40 dark:text-indigo-300' },
      'Delivered': { icon: '🏠', text: 'Teslim Edildi', color: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300' },
      'Cancelled': { icon: '❌', text: 'İptal Edildi', color: 'bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-300' }
    };
    const c = config[status] || config['Pending'];
    return <span className={`px-2 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
  };

  const getPaymentStatusBadge = (status) => {
    const config = {
      'Pending': { icon: '⏳', text: 'Beklemede', color: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300' },
      'Partial': { icon: '🔄', text: 'Kısmen Ödendi', color: 'bg-orange-100 text-orange-800 dark:bg-orange-900/40 dark:text-orange-300' },
      'Paid': { icon: '✅', text: 'Ödendi', color: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300' },
      'Cancelled': { icon: '❌', text: 'İptal', color: 'bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-300' }
    };
    const c = config[status] || config['Pending'];
    return <span className={`px-2 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
  };

  const formatPrice = (price) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
      minimumFractionDigits: 2
    }).format(price);
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Siparişler</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} sipariş</p>
        </div>
        {isAdmin && (
          <Link
            to="/orders/create"
            className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg transition-colors flex items-center gap-2"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Yeni Sipariş
          </Link>
        )}
      </div>

      {/* Filters */}
      <div className="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-800 p-4 mb-6">
        <div className="flex flex-wrap gap-3">
          <input
            type="text"
            placeholder="Sipariş No, Müşteri ara..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            className="flex-1 min-w-[200px] px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200 text-sm"
          />
          <select
            value={customerIdFilter}
            onChange={(e) => setCustomerIdFilter(e.target.value)}
            className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500"
          >
            <option value="">Tüm Müşteriler</option>
            {customers.map(c => <option key={c.id} value={c.id}>{c.name || `${c.firstName} ${c.lastName}`}</option>)}
          </select>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500"
          >
            <option value="">Tüm Durumlar</option>
            {statusOptions.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
          </select>
          <select
            value={paymentStatusFilter}
            onChange={(e) => setPaymentStatusFilter(e.target.value)}
            className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500"
          >
            <option value="">Tüm Ödeme Durumları</option>
            {paymentStatusOptions.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
          </select>
          <button 
            onClick={handleSearch} 
            className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg transition-colors text-sm font-medium"
          >
            Ara
          </button>
          <button 
            onClick={handleClearFilters} 
            className="bg-gray-500 hover:bg-gray-600 dark:bg-gray-700 dark:hover:bg-gray-600 text-white px-4 py-2 rounded-lg transition-colors text-sm font-medium"
          >
            Temizle
          </button>
        </div>
      </div>

      {/* Orders Table */}
      <div className="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-800 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-800">
            <thead className="bg-gray-50 dark:bg-gray-800/50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sipariş No</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Müşteri</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tarih</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Toplam</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Ödeme</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
              {orders.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                    Sipariş bulunmuyor
                  </td>
                </tr>
              ) : (
                orders.map((order) => (
                  <tr key={order.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                    <td className="px-6 py-4 font-mono text-sm font-medium text-indigo-600 dark:text-indigo-400">
                      {order.orderNumber}
                    </td>
                    <td className="px-6 py-4 text-gray-900 dark:text-gray-100">
                      {order.customerName || '-'}
                    </td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-400 text-sm">
                      {new Date(order.orderDate).toLocaleDateString('tr-TR')}
                    </td>
                    <td className="px-6 py-4 font-semibold text-gray-900 dark:text-white">
                      {formatPrice(order.totalAmount)}
                    </td>
                    <td className="px-6 py-4">{getStatusBadge(order.status)}</td>
                    <td className="px-6 py-4">{getPaymentStatusBadge(order.paymentStatus)}</td>
                    <td className="px-6 py-4 text-center">
                      <div className="flex justify-center gap-2">
                        <Link
                          to={`/orders/${order.id}`}
                          state={{ from: '/orders' }}
                          className="p-2 text-emerald-600 dark:text-emerald-400 hover:bg-emerald-50 dark:hover:bg-emerald-950/30 rounded-lg transition-colors"
                          title="Detay"
                        >
                          👁️
                        </Link>
                        {isAdmin && (
                          <>
                            <Link
                              to={`/orders/edit/${order.id}`}
                              className="p-2 text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-950/30 rounded-lg transition-colors"
                              title="Düzenle"
                            >
                              ✏️
                            </Link>
                            <button
                              onClick={() => handleDelete(order.id, order.orderNumber)}
                              className="p-2 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-950/30 rounded-lg transition-colors"
                              title="Sil"
                            >
                              🗑️
                            </button>
                          </>
                        )}
                      </div>
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
        <div className="flex justify-between items-center mt-4 pt-2">
          <div className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} kayıt</div>
          <div className="flex gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="px-3 py-1 border border-gray-200 dark:border-gray-700 rounded-lg disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors text-gray-700 dark:text-gray-300"
            >
              ◀
            </button>
            <span className="px-3 py-1 text-sm text-gray-700 dark:text-gray-300">Sayfa {page} / {totalPages}</span>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="px-3 py-1 border border-gray-200 dark:border-gray-700 rounded-lg disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors text-gray-700 dark:text-gray-300"
            >
              ▶
            </button>
          </div>
        </div>
      )}
    </div>
  );
}