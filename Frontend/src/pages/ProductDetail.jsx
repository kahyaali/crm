import { useState, useEffect } from 'react';
import { useParams, Link, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import toast from 'react-hot-toast';
import api from '../services/api';
import { getImageUrl } from '../helpers/imageHelper';

export default function ProductDetail() {
  const { id } = useParams();
  const location = useLocation();
  const { user } = useAuth();
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);

  const hasPermission = (permission) => {
    return user?.role === 'SystemAdmin' || user?.role === 'Admin';
  };

  // Nereden geldiğini tespit et
  const fromPage = location.state?.from;
  const referrer = document.referrer;
  
  let backLink = '/products';
  let backText = '← Ürünlere Dön';
  
  if (fromPage === '/products') {
    backLink = '/products';
    backText = '← Tüm Ürünlere Dön';
  } else if (referrer?.includes('/products')) {
    backLink = '/products';
    backText = '← Ürünlere Dön';
  }

  useEffect(() => {
    fetchProductDetail();
  }, [id]);

  const fetchProductDetail = async () => {
    try {
      setLoading(true);
      const response = await api.get(`/Products/${id}`);
      setProduct(response.data);
    } catch (error) {
      console.error('Hata:', error);
      toast.error('Ürün bilgileri yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const formatPrice = (price, currency) => {
    if (!price) return '-';
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency || 'TRY',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(price);
  };

  const getStockStatus = (stock, minStockLevel, maxStockLevel) => {
    if (stock <= 0) {
      return { text: 'Stokta Yok', color: 'red' };
    }
    if (minStockLevel && stock <= minStockLevel) {
      return { text: 'Kritik Stok', color: 'orange' };
    }
    if (maxStockLevel && stock >= maxStockLevel) {
      return { text: 'Maksimum Stok', color: 'yellow' };
    }
    return { text: 'Stokta Var', color: 'green' };
  };

  const statusColor = {
    red: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300',
    orange: 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-300',
    yellow: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300',
    green: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
  };

  const InfoCard = ({ icon, title, value, color = 'blue' }) => {
    const colorClasses = {
      blue: 'bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 border border-blue-100 dark:border-blue-800/30',
      green: 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-300 border border-green-100 dark:border-green-800/30',
      purple: 'bg-purple-50 dark:bg-purple-900/20 text-purple-700 dark:text-purple-300 border border-purple-100 dark:border-purple-800/30',
      orange: 'bg-orange-50 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300 border border-orange-100 dark:border-orange-800/30',
      red: 'bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 border border-red-100 dark:border-red-800/30',
      gray: 'bg-gray-50 dark:bg-gray-800/50 text-gray-700 dark:text-gray-300 border border-gray-100 dark:border-gray-700',
      emerald: 'bg-emerald-50 dark:bg-emerald-900/20 text-emerald-700 dark:text-emerald-300 border border-emerald-100 dark:border-emerald-800/30',
      amber: 'bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-300 border border-amber-100 dark:border-amber-800/30'
    };
    return (
      <div className={`p-4 rounded-xl ${colorClasses[color]} transition-all hover:shadow-md`}>
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

  if (!product) {
    return (
      <div className="text-center py-16">
        <div className="text-6xl mb-4">🔍</div>
        <p className="text-gray-500 dark:text-gray-400">Ürün bulunamadı</p>
        <Link to={backLink} className="inline-flex items-center gap-2 mt-4 text-blue-600 hover:text-blue-700 font-medium">
          ← Geri Dön
        </Link>
      </div>
    );
  }

  const stockStatus = getStockStatus(product.stockQuantity, product.minStockLevel, product.maxStockLevel);
  const brandName = product.brandName || product.brand?.name || '-';
  const categoryName = product.categoryName || product.category?.name || '-';

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 via-white to-gray-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900">
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
        <div className="relative bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 rounded-2xl overflow-hidden shadow-2xl mb-8">
          <div className="absolute inset-0 bg-black/20"></div>
          <div className="relative p-8 flex flex-col md:flex-row items-center gap-6">
            {/* Ürün Resmi */}
            <div className="w-32 h-32 bg-white/20 rounded-2xl backdrop-blur-sm flex items-center justify-center p-2 shadow-xl border border-white/20">
              {product.imageUrl ? (
                <img 
                  src={getImageUrl(product.imageUrl)} 
                  alt={product.name} 
                  className="w-full h-full object-contain rounded-xl"
                  onError={(e) => { e.target.src = 'https://placehold.co/128x128?text=Ürün' }}
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center text-5xl text-white/70">
                  📦
                </div>
              )}
            </div>
            <div className="flex-1 text-center md:text-left">
              <div className="flex flex-wrap items-center gap-3 justify-center md:justify-start mb-2">
                <h1 className="text-3xl md:text-4xl font-bold text-white">
                  {product.name}
                </h1>
                <span className={`px-3 py-1 rounded-full text-xs font-medium ${product.isActive ? 'bg-green-500/30 text-green-100' : 'bg-red-500/30 text-red-100'}`}>
                  {product.isActive ? '✅ Aktif' : '❌ Pasif'}
                </span>
              </div>
              <div className="flex flex-wrap gap-2 justify-center md:justify-start mt-3">
                <span className="px-3 py-1 bg-white/20 rounded-full text-sm text-white backdrop-blur-sm">
                  SKU: {product.sku || '-'}
                </span>
                {product.barcode && (
                  <span className="px-3 py-1 bg-white/20 rounded-full text-sm text-white backdrop-blur-sm">
                    Barkod: {product.barcode}
                  </span>
                )}
                <span className="px-3 py-1 bg-white/20 rounded-full text-sm text-white backdrop-blur-sm">
                  Stok: {product.stockQuantity} {product.unit || 'Adet'}
                </span>
              </div>
            </div>
           
          </div>
        </div>

        {/* Detay Kartları - 4'lü grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          <InfoCard icon="🏷️" title="Ürün Adı" value={product.name} color="blue" />
          <InfoCard icon="💰" title="Fiyat" value={formatPrice(product.price, product.currency)} color="green" />
          <InfoCard icon="📦" title="Stok Durumu" value={stockStatus.text} color={stockStatus.color} />
          <InfoCard icon="🏭" title="Marka" value={brandName} color="purple" />
        </div>

        {/* Detaylı Bilgiler */}
        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg overflow-hidden border border-gray-100 dark:border-gray-700">
          <div className="p-6 space-y-6">
            
            {/* Temel Bilgiler */}
            <div>
              <SectionTitle icon="📋" title="Temel Bilgiler" />
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                <InfoCard icon="🔢" title="SKU" value={product.sku || '-'} color="gray" />
                <InfoCard icon="📊" title="Barkod" value={product.barcode || '-'} color="gray" />
                <InfoCard icon="📏" title="Birim" value={product.unit || 'Adet'} color="gray" />
                <InfoCard icon="📁" title="Kategori" value={categoryName} color="purple" />
              </div>
            </div>

            {/* Stok Bilgileri */}
            <div>
              <SectionTitle icon="📊" title="Stok Bilgileri" />
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                <InfoCard icon="📦" title="Mevcut Stok" value={`${product.stockQuantity} ${product.unit || 'Adet'}`} color="blue" />
                <InfoCard icon="⚠️" title="Minimum Stok" value={product.minStockLevel ? `${product.minStockLevel} ${product.unit || 'Adet'}` : '-'} color="orange" />
                <InfoCard icon="📈" title="Maksimum Stok" value={product.maxStockLevel ? `${product.maxStockLevel} ${product.unit || 'Adet'}` : '-'} color="green" />
                <InfoCard icon="🔍" title="Stok Takibi" value={product.isStockTrackable ? '✅ Aktif' : '❌ Pasif'} color={product.isStockTrackable ? 'emerald' : 'gray'} />
              </div>
            </div>

            {/* Açıklama */}
            {product.description && (
              <div>
                <SectionTitle icon="📝" title="Ürün Açıklaması" />
                <div className="bg-gray-50 dark:bg-gray-700/30 rounded-xl p-4 border border-gray-100 dark:border-gray-700">
                  <p className="text-gray-700 dark:text-gray-300 whitespace-pre-wrap leading-relaxed">
                    {product.description}
                  </p>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="mt-6 text-center">
          <p className="text-xs text-gray-400 dark:text-gray-500">
            Oluşturma: {product.createdAt ? new Date(product.createdAt).toLocaleString('tr-TR') : '-'}
            {product.updatedAt && ` | Son güncelleme: ${new Date(product.updatedAt).toLocaleString('tr-TR')}`}
          </p>
        </div>
      </div>
    </div>
  );
}