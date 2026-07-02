import { useState, useEffect } from 'react';
import { useParams, Link, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';

export default function OrderDetail() {
  const { id } = useParams();
  const location = useLocation();
  const { user } = useAuth();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [updating, setUpdating] = useState(false);
  const [statusOptions, setStatusOptions] = useState([]);
  const [paymentStatusOptions, setPaymentStatusOptions] = useState([]);

  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';

  const fromPage = location.state?.from;
  let backLink = '/orders';
  let backText = '← Siparişlere Dön';

  if (fromPage === '/orders') {
    backLink = '/orders';
    backText = '← Tüm Siparişlere Dön';
  }

  useEffect(() => {
    fetchOrderDetail();
    fetchStatusLists();
  }, [id]);

  const fetchOrderDetail = async () => {
    try {
      setLoading(true);
      const response = await api.get(`/Orders/${id}`);
      setOrder(response.data);
    } catch (error) {
      console.error('Hata:', error);
      toast.error('Sipariş bilgileri yüklenemedi');
    } finally {
      setLoading(false);
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

  const handleStatusUpdate = async (newStatus) => {
    if (!isAdmin) return;
    
    const result = await Swal.fire({
      title: 'Durumu Güncelle',
      text: `Sipariş durumunu "${newStatus}" olarak değiştirmek istediğinize emin misiniz?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Güncelle',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      setUpdating(true);
      try {
        await api.patch(`/Orders/${id}/status`, newStatus, {
          headers: { 'Content-Type': 'application/json' }
        });
        toast.success('Sipariş durumu güncellendi');
        fetchOrderDetail();
      } catch (error) {
        toast.error(error.response?.data?.message || 'Güncelleme hatası');
      } finally {
        setUpdating(false);
      }
    }
  };

  const getStatusBadge = (status) => {
    const config = {
      'Pending': { icon: '⏳', text: 'Beklemede', color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300' },
      'Approved': { icon: '✅', text: 'Onaylandı', color: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300' },
      'Preparing': { icon: '📦', text: 'Hazırlanıyor', color: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300' },
      'Shipped': { icon: '🚚', text: 'Kargolandı', color: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/30 dark:text-indigo-300' },
      'Delivered': { icon: '🏠', text: 'Teslim Edildi', color: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300' },
      'Cancelled': { icon: '❌', text: 'İptal Edildi', color: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300' }
    };
    const c = config[status] || config['Pending'];
    return <span className={`px-2 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
  };

  const getPaymentStatusBadge = (status) => {
    const config = {
      'Pending': { icon: '⏳', text: 'Beklemede', color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30' },
      'Partial': { icon: '🔄', text: 'Kısmen Ödendi', color: 'bg-orange-100 text-orange-800 dark:bg-orange-900/30' },
      'Paid': { icon: '✅', text: 'Ödendi', color: 'bg-green-100 text-green-800 dark:bg-green-900/30' },
      'Cancelled': { icon: '❌', text: 'İptal', color: 'bg-red-100 text-red-800 dark:bg-red-900/30' }
    };
    const c = config[status] || config['Pending'];
    return <span className={`px-2 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
  };

  // 🔴 DÜZELTİLDİ - Para birimine göre formatla
  const formatPrice = (price, currency = 'TRY') => {
    if (!price) return '-';
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 2
    }).format(price);
  };

  const InfoCard = ({ icon, title, value, color = 'blue' }) => {
    const colors = {
      blue: 'bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300',
      green: 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-300',
      purple: 'bg-purple-50 dark:bg-purple-900/20 text-purple-700 dark:text-purple-300',
      orange: 'bg-orange-50 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300'
    };
    return (
      <div className={`p-4 rounded-xl ${colors[color]} border border-gray-100 dark:border-gray-700`}>
        <div className="flex items-center gap-3">
          <div className="text-2xl">{icon}</div>
          <div>
            <p className="text-xs opacity-70 uppercase tracking-wide">{title}</p>
            <p className="text-sm font-semibold mt-0.5 break-words">{value || '-'}</p>
          </div>
        </div>
      </div>
    );
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-96">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (!order) {
    return (
      <div className="text-center py-16">
        <div className="text-6xl mb-4">🔍</div>
        <p className="text-gray-500">Sipariş bulunamadı</p>
        <Link to={backLink} className="inline-flex items-center gap-2 mt-4 text-blue-600 hover:text-blue-700">
          ← Geri Dön
        </Link>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800">
      <div className="max-w-6xl mx-auto px-4 py-8">
        
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
          <div className="relative p-8">
            <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
              <div>
                <div className="flex items-center gap-3 mb-2">
                  <h1 className="text-2xl md:text-3xl font-bold text-white">
                    Sipariş #{order.orderNumber}
                  </h1>
                  {getStatusBadge(order.status)}
                </div>
                <p className="text-blue-100">
                  {new Date(order.orderDate).toLocaleString('tr-TR')}
                </p>
              </div>
              <div className="text-right">
                <p className="text-white/80 text-sm">Toplam Tutar ({order.currency || 'TRY'})</p>
                <p className="text-3xl font-bold text-white">{formatPrice(order.totalAmount, order.currency)}</p>
              </div>
            </div>
          </div>
        </div>

        {/* Info Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          <InfoCard icon="👤" title="Müşteri" value={order.customerName} color="blue" />
          <InfoCard icon="💰" title="Alt Toplam" value={formatPrice(order.subTotal, order.currency)} color="green" />
          <InfoCard icon="📊" title="KDV" value={formatPrice(order.taxAmount, order.currency)} color="purple" />
          <InfoCard icon="💳" title="Ödeme Durumu" value={getPaymentStatusBadge(order.paymentStatus)} color="orange" />
        </div>

        {/* Status Update (Admin only) */}
        {isAdmin && (
          <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6 mb-8 border border-gray-100 dark:border-gray-700">
            <div className="flex items-center justify-between flex-wrap gap-4">
              <div>
                <h3 className="text-lg font-semibold text-gray-800 dark:text-white mb-1">Sipariş Durumu</h3>
                <p className="text-sm text-gray-500">Sipariş durumunu güncelleyin</p>
              </div>
              <div className="flex gap-2">
                {statusOptions.map(opt => (
                  <button
                    key={opt.value}
                    onClick={() => handleStatusUpdate(opt.value)}
                    disabled={updating || order.status === opt.value}
                    className={`px-4 py-2 rounded-lg text-sm font-medium transition-all ${
                      order.status === opt.value
                        ? 'bg-green-600 text-white cursor-default'
                        : 'bg-gray-200 text-gray-700 hover:bg-blue-600 hover:text-white dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-blue-600'
                    }`}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Order Items Table */}
        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg overflow-hidden border border-gray-100 dark:border-gray-700">
          <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
            <h3 className="text-lg font-semibold text-gray-800 dark:text-white">Sipariş Kalemleri</h3>
          </div>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead className="bg-gray-50 dark:bg-gray-700/50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Ürün</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">SKU</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Adet</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Birim Fiyat ({order.currency || 'TRY'})</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Toplam ({order.currency || 'TRY'})</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                {order.items?.map((item, index) => (
                  <tr key={index} className="hover:bg-gray-50 dark:hover:bg-gray-700/30">
                    <td className="px-6 py-4 text-gray-800 dark:text-white">{item.productName || `Ürün #${item.productId}`}</td>
                    <td className="px-6 py-4 text-sm text-gray-500 dark:text-gray-400">{item.productSku || '-'}</td>
                    <td className="px-6 py-4 text-right font-medium text-gray-700 dark:text-gray-300">{item.quantity}</td>
                    <td className="px-6 py-4 text-right text-gray-700 dark:text-gray-300">
                      {formatPrice(item.unitPrice, order.currency)}
                    </td>
                    <td className="px-6 py-4 text-right font-semibold text-gray-800 dark:text-white">
                      {formatPrice(item.totalPrice, order.currency)}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="bg-gray-50 dark:bg-gray-700/50">
                <tr>
                  <td colSpan={4} className="px-6 py-3 text-right font-semibold text-gray-700 dark:text-gray-300">Alt Toplam ({order.currency || 'TRY'})</td>
                  <td className="px-6 py-3 text-right font-semibold text-gray-800 dark:text-white">{formatPrice(order.subTotal, order.currency)}</td>
                </tr>
                <tr className="border-t border-gray-200 dark:border-gray-700">
                  <td colSpan={4} className="px-6 py-3 text-right font-semibold text-gray-700 dark:text-gray-300">KDV (%20)</td>
                  <td className="px-6 py-3 text-right font-semibold text-gray-800 dark:text-white">{formatPrice(order.taxAmount, order.currency)}</td>
                </tr>
                <tr className="bg-blue-50 dark:bg-blue-900/20">
                  <td colSpan={4} className="px-6 py-3 text-right font-bold text-gray-800 dark:text-white">Genel Toplam ({order.currency || 'TRY'})</td>
                  <td className="px-6 py-3 text-right font-bold text-blue-700 dark:text-blue-400 text-lg">{formatPrice(order.totalAmount, order.currency)}</td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>

        {/* Shipping Address & Notes */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-6">
          {order.shippingAddress && (
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6 border border-gray-100 dark:border-gray-700">
              <h3 className="text-lg font-semibold text-gray-800 dark:text-white mb-3 flex items-center gap-2">
                🚚 Teslimat Adresi
              </h3>
              <p className="text-gray-600 dark:text-gray-300 whitespace-pre-wrap">{order.shippingAddress}</p>
            </div>
          )}
          {order.notes && (
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6 border border-gray-100 dark:border-gray-700">
              <h3 className="text-lg font-semibold text-gray-800 dark:text-white mb-3 flex items-center gap-2">
                📝 Sipariş Notu
              </h3>
              <p className="text-gray-600 dark:text-gray-300 whitespace-pre-wrap">{order.notes}</p>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="mt-6 text-center">
          <p className="text-xs text-gray-400">
            Oluşturma: {new Date(order.createdAt).toLocaleString('tr-TR')}
            {order.updatedAt && ` | Son güncelleme: ${new Date(order.updatedAt).toLocaleString('tr-TR')}`}
          </p>
        </div>
      </div>
    </div>
  );
}