import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';

export default function EditOrder() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);
  const [pageLoading, setPageLoading] = useState(true);
  const [customers, setCustomers] = useState([]);
  const [products, setProducts] = useState([]);
  const [originalOrder, setOriginalOrder] = useState(null);
  
  const [rates, setRates] = useState({ TRY: 1 });
  const [ratesLoading, setRatesLoading] = useState(true);
  
  const [formData, setFormData] = useState({
    id: '',
    customerId: '',
    orderDate: '',
    deliveryDate: '',
    shippingAddress: '',
    notes: '',
    status: 'Pending',
    paymentStatus: 'Pending',
    currency: 'TRY',
    taxRate: 20,
    items: []
  });

  const [currentItem, setCurrentItem] = useState({
    productId: '',
    quantity: 1,
    unitPrice: 0,
    originalPrice: 0,
    productCurrency: 'TRY',
    editingIndex: null
  });

  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';

  const currencies = [
    { code: 'TRY', symbol: '₺', name: 'Türk Lirası' },
    { code: 'USD', symbol: '$', name: 'Amerikan Doları' },
    { code: 'EUR', symbol: '€', name: 'Euro' },
    { code: 'GBP', symbol: '£', name: 'İngiliz Sterlini' }
  ];

  const statusOptions = [
    { value: 'Pending', label: '⏳ Beklemede' },
    { value: 'Approved', label: '✅ Onaylandı' },
    { value: 'Preparing', label: '📦 Hazırlanıyor' },
    { value: 'Shipped', label: '🚚 Kargolandı' },
    { value: 'Delivered', label: '🏠 Teslim Edildi' },
    { value: 'Cancelled', label: '❌ İptal Edildi' }
  ];

  const paymentStatusOptions = [
    { value: 'Pending', label: '⏳ Beklemede' },
    { value: 'Partial', label: '🔄 Kısmen Ödendi' },
    { value: 'Paid', label: '✅ Ödendi' },
    { value: 'Cancelled', label: '❌ İptal' }
  ];

  const convertCurrency = (amount, fromCurrency, toCurrency) => {
    if (!amount) return 0;
    if (fromCurrency === toCurrency) return amount;
    const fromRateInTry = rates[fromCurrency] || 1;
    const toRateInTry = rates[toCurrency] || 1;
    const amountInTRY = amount * fromRateInTry;
    const finalAmount = amountInTRY / toRateInTry;
    return parseFloat(finalAmount.toFixed(4));
  };

  const fetchExchangeRates = async () => {
    try {
      setRatesLoading(true);
      const response = await api.get('/ExchangeRateSettings/all-rates');
      if (response.data) {
        setRates({ ...response.data, TRY: 1 });
      }
    } catch (error) {
      console.error('Kurlar alınamadı:', error);
      setRates({ TRY: 1, USD: 35.0, EUR: 38.0, GBP: 44.0 });
    } finally {
      setRatesLoading(false);
    }
  };

  const handleOrderCurrencyChange = (newCurrency) => {
    const updatedItems = formData.items.map(item => {
      const newUnitPrice = convertCurrency(item.originalPrice, item.productCurrency, newCurrency);
      return {
        ...item,
        unitPrice: newUnitPrice,
        totalPrice: item.quantity * newUnitPrice
      };
    });
    setFormData(prev => ({ ...prev, currency: newCurrency, items: updatedItems }));
    if (currentItem.productId) {
      const convertedPrice = convertCurrency(currentItem.originalPrice, currentItem.productCurrency, newCurrency);
      setCurrentItem(prev => ({ ...prev, unitPrice: convertedPrice }));
    }
  };

  useEffect(() => {
    if (!isAdmin) {
      toast.error('Sipariş düzenleme yetkiniz yok');
      navigate('/orders');
      return;
    }
    fetchExchangeRates();
    fetchCustomers();
    fetchProducts();
    fetchOrderDetail();
  }, [id]);

  const fetchOrderDetail = async () => {
    try {
      setPageLoading(true);
      const response = await api.get(`/Orders/${id}`);
      const order = response.data;
      setOriginalOrder(order);
      setFormData({
        id: order.id,
        customerId: order.customerId,
        orderDate: order.orderDate?.split('T')[0] || '',
        deliveryDate: order.deliveryDate?.split('T')[0] || '',
        shippingAddress: order.shippingAddress || '',
        notes: order.notes || '',
        status: order.status || 'Pending',
        paymentStatus: order.paymentStatus || 'Pending',
        currency: order.currency || 'TRY',
        taxRate: 20,
        items: order.items?.map(item => ({
          id: item.id,
          productId: item.productId,
          productName: item.productName,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          productCurrency: order.currency || 'TRY',
          originalPrice: item.unitPrice,
          totalPrice: item.totalPrice
        })) || []
      });
    } catch (error) {
      console.error('Hata:', error);
      toast.error('Sipariş bilgileri yüklenemedi');
      navigate('/orders');
    } finally {
      setPageLoading(false);
    }
  };

  const fetchCustomers = async () => {
    try {
      const response = await api.get('/Orders/customer-list');
      setCustomers(response.data || []);
    } catch (error) {
      toast.error('Müşteriler yüklenemedi');
    }
  };

  const fetchProducts = async () => {
    try {
      const response = await api.get('/Products', { params: { pageSize: 100, isActive: true } });
      setProducts(response.data.data || []);
    } catch (error) {
      toast.error('Ürünler yüklenemedi');
    }
  };

  const handleEditItem = (index) => {
    const item = formData.items[index];
    setCurrentItem({
      productId: item.productId.toString(),
      quantity: item.quantity,
      unitPrice: item.unitPrice,
      originalPrice: item.originalPrice,
      productCurrency: item.productCurrency,
      editingIndex: index
    });
    setTimeout(() => {
      document.getElementById('product-select')?.scrollIntoView({ behavior: 'smooth' });
    }, 100);
  };

  const handleAddOrUpdateItem = () => {
    if (!currentItem.productId) {
      toast.error('Lütfen bir ürün seçin');
      return;
    }
    if (currentItem.quantity < 1) {
      toast.error('Miktar 1\'den küçük olamaz');
      return;
    }
    if (currentItem.unitPrice <= 0) {
      toast.error('Birim fiyat 0\'dan büyük olmalıdır');
      return;
    }

    const product = products.find(p => p.id === parseInt(currentItem.productId));
    
    if (currentItem.editingIndex !== null) {
      const updatedItems = [...formData.items];
      updatedItems[currentItem.editingIndex] = {
        ...updatedItems[currentItem.editingIndex],
        productId: parseInt(currentItem.productId),
        productName: product?.name,
        quantity: currentItem.quantity,
        unitPrice: currentItem.unitPrice,
        productCurrency: currentItem.productCurrency,
        originalPrice: currentItem.originalPrice,
        totalPrice: currentItem.quantity * currentItem.unitPrice
      };
      setFormData(prev => ({ ...prev, items: updatedItems }));
      toast.success('Ürün güncellendi');
    } 
    else {
      const isAlreadyAdded = formData.items.some(item => item.productId === parseInt(currentItem.productId));
      if (isAlreadyAdded) {
        toast.error('Bu ürün zaten listeye eklenmiş.');
        return;
      }
      
      const newItem = {
        id: null,
        productId: parseInt(currentItem.productId),
        productName: product?.name,
        quantity: currentItem.quantity,
        unitPrice: currentItem.unitPrice,
        productCurrency: currentItem.productCurrency,
        originalPrice: currentItem.originalPrice,
        totalPrice: currentItem.quantity * currentItem.unitPrice
      };
      setFormData(prev => ({ ...prev, items: [...prev.items, newItem] }));
      toast.success('Ürün eklendi');
    }
    
    setCurrentItem({ 
      productId: '', 
      quantity: 1, 
      unitPrice: 0, 
      originalPrice: 0, 
      productCurrency: 'TRY',
      editingIndex: null 
    });
  };

  const handleRemoveItem = (index) => {
    const item = formData.items[index];
    if (item.id) {
      Swal.fire({
        title: 'Ürünü Kaldır',
        text: 'Bu ürünü siparişten kaldırmak istediğinize emin misiniz?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Evet, Kaldır',
        cancelButtonText: 'İptal'
      }).then(async (result) => {
        if (result.isConfirmed) {
          setFormData(prev => ({ ...prev, items: prev.items.filter((_, i) => i !== index) }));
          toast.success('Ürün kaldırıldı');
        }
      });
    } else {
      setFormData(prev => ({ ...prev, items: prev.items.filter((_, i) => i !== index) }));
    }
  };

  const handleProductChange = (productId) => {
    const product = products.find(p => p.id === parseInt(productId));
    if (product) {
      const convertedPrice = convertCurrency(product.price, product.currency || 'TRY', formData.currency);
      setCurrentItem(prev => ({
        ...prev,
        productId: productId,
        quantity: 1,
        unitPrice: convertedPrice,
        originalPrice: product.price,
        productCurrency: product.currency || 'TRY'
      }));
    }
  };

  const getCurrencySymbol = (currencyCode) => {
    const c = currencies.find(c => c.code === currencyCode);
    return c?.symbol || '₺';
  };

  const formatPrice = (price, currencyCode) => {
    const symbol = getCurrencySymbol(currencyCode);
    return `${price.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })} ${symbol}`;
  };

  const calculateSubTotal = () => formData.items.reduce((sum, item) => sum + item.totalPrice, 0);
  const calculateTax = () => calculateSubTotal() * (formData.taxRate / 100);
  const calculateTotal = () => calculateSubTotal() + calculateTax();

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.customerId) {
      toast.error('Lütfen bir müşteri seçin');
      return;
    }
    if (formData.items.length === 0) {
      toast.error('En az bir ürün eklemelisiniz');
      return;
    }

    setLoading(true);
    try {
      const submitData = {
        id: parseInt(formData.id),
        customerId: parseInt(formData.customerId),
        orderDate: formData.orderDate,
        deliveryDate: formData.deliveryDate || null,
        shippingAddress: formData.shippingAddress || null,
        notes: formData.notes || null,
        status: formData.status,
        paymentStatus: formData.paymentStatus,
        currency: formData.currency,
        taxRate: formData.taxRate / 100,
        items: formData.items.map(item => ({
          id: item.id || null,
          productId: item.productId,
          quantity: item.quantity,
          unitPrice: item.unitPrice
        }))
      };
      await api.put(`/Orders/${id}`, submitData);
      toast.success('✅ Sipariş başarıyla güncellendi');
      navigate('/orders');
    } catch (error) {
      toast.error(error.response?.data?.message || 'Sipariş güncellenemedi');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    const config = {
      'Pending': 'bg-amber-50 text-amber-700 dark:bg-amber-950/40 dark:text-amber-400',
      'Approved': 'bg-sky-50 text-sky-700 dark:bg-sky-950/40 dark:text-sky-400',
      'Preparing': 'bg-purple-50 text-purple-700 dark:bg-purple-950/40 dark:text-purple-400',
      'Shipped': 'bg-indigo-50 text-indigo-700 dark:bg-indigo-950/40 dark:text-indigo-400',
      'Delivered': 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-400',
      'Cancelled': 'bg-rose-50 text-rose-700 dark:bg-rose-950/40 dark:text-rose-400'
    };
    return `inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-semibold ${config[status] || config['Pending']}`;
  };

  const inputStyle = "w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200 text-sm";
  const labelStyle = "text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block";

  if (pageLoading || ratesLoading) {
    return (
      <div className="flex flex-col justify-center items-center min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="animate-spin rounded-full h-14 w-14 border-4 border-indigo-500/20 border-t-indigo-600"></div>
        <p className="mt-4 text-sm font-medium text-gray-500 dark:text-gray-400 animate-pulse">
          {ratesLoading ? 'Canlı kurlar yükleniyor...' : 'Sipariş verileri çözümleniyor...'}
        </p>
      </div>
    );
  }

  if (!isAdmin) return null;

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950 px-4 py-6">
      <div className="max-w-6xl mx-auto">
        
        {/* Header */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 mb-5 pb-3 border-b border-gray-200 dark:border-gray-800">
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">Sipariş Düzenle</h1>
              <span className={getStatusBadge(formData.status)}>{formData.status}</span>
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
              Sipariş #{originalOrder?.orderNumber} — {originalOrder?.customerName}
              {!ratesLoading && rates.USD && <span className="ml-2 text-green-600 dark:text-green-400">🟢 Canlı Kur</span>}
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Link 
              to="/orders" 
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition"
            >
              <span>←</span> Listeye Dön
            </Link>
            <Link 
              to={`/orders/${id}`} 
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-indigo-700 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-950/40 border border-indigo-200 dark:border-indigo-800 rounded-lg hover:bg-indigo-100 dark:hover:bg-indigo-950/60 transition"
            >
              <span>👁️</span> Detaya Dön
            </Link>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          
          {/* Sipariş Bilgileri */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl p-5 shadow-sm">
            <div className="flex items-center gap-2 pb-2 mb-4 border-b border-gray-100 dark:border-gray-800">
              <span className="text-lg">📝</span>
              <h2 className="text-base font-semibold text-gray-800 dark:text-gray-100">Sipariş Bilgileri</h2>
            </div>
            
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className={labelStyle}>Müşteri <span className="text-red-500">*</span></label>
                <select value={formData.customerId} onChange={(e) => setFormData({ ...formData, customerId: e.target.value })} className={inputStyle} required>
                  <option value="">Müşteri Seçin</option>
                  {customers.map(c => (
                    <option key={c.id} value={c.id}>{c.firstName} {c.lastName} {c.companyName ? `(${c.companyName})` : ''}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelStyle}>Para Birimi</label>
                <select value={formData.currency} onChange={(e) => handleOrderCurrencyChange(e.target.value)} className={inputStyle} disabled={ratesLoading}>
                  {currencies.map(c => <option key={c.code} value={c.code}>{c.symbol} {c.name}</option>)}
                </select>
              </div>
              <div>
                <label className={labelStyle}>KDV Oranı (%)</label>
                <input type="number" step="1" min="0" max="100" value={formData.taxRate} onChange={(e) => setFormData({ ...formData, taxRate: parseFloat(e.target.value) || 0 })} className={inputStyle} />
              </div>
              <div>
                <label className={labelStyle}>Sipariş Durumu</label>
                <select value={formData.status} onChange={(e) => setFormData({ ...formData, status: e.target.value })} className={inputStyle}>
                  {statusOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                </select>
              </div>
              <div>
                <label className={labelStyle}>Ödeme Durumu</label>
                <select value={formData.paymentStatus} onChange={(e) => setFormData({ ...formData, paymentStatus: e.target.value })} className={inputStyle}>
                  {paymentStatusOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                </select>
              </div>
              <div className="flex gap-3">
                <div className="flex-1">
                  <label className={labelStyle}>Sipariş Tarihi</label>
                  <input type="date" value={formData.orderDate} onChange={(e) => setFormData({ ...formData, orderDate: e.target.value })} className={inputStyle} />
                </div>
                <div className="flex-1">
                  <label className={labelStyle}>Teslimat Tarihi</label>
                  <input type="date" value={formData.deliveryDate} onChange={(e) => setFormData({ ...formData, deliveryDate: e.target.value })} className={inputStyle} />
                </div>
              </div>
            </div>
          </div>

          {/* Teslimat & Notlar */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl p-5 shadow-sm">
            <div className="flex items-center gap-2 pb-2 mb-4 border-b border-gray-100 dark:border-gray-800">
              <span className="text-lg">📍</span>
              <h2 className="text-base font-semibold text-gray-800 dark:text-gray-100">Teslimat & Notlar</h2>
            </div>
            <div className="space-y-4">
              <div>
                <label className={labelStyle}>Teslimat Adresi</label>
                <textarea rows="2" value={formData.shippingAddress} onChange={(e) => setFormData({ ...formData, shippingAddress: e.target.value })} className={`${inputStyle} resize-none`} placeholder="Sevk adresi..." />
              </div>
              <div>
                <label className={labelStyle}>Sipariş Notları</label>
                <textarea rows="2" value={formData.notes} onChange={(e) => setFormData({ ...formData, notes: e.target.value })} className={`${inputStyle} resize-none`} placeholder="Siparişe özel notlar..." />
              </div>
            </div>
          </div>

          {/* Ürünler Bölümü */}
          <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl p-5 shadow-sm">
            <div className="flex items-center gap-2 pb-2 mb-4 border-b border-gray-100 dark:border-gray-800">
              <span className="text-lg">📦</span>
              <h2 className="text-base font-semibold text-gray-800 dark:text-gray-100">Sipariş Kalemleri</h2>
            </div>
            
            {/* Ürün Ekleme/Güncelleme Formu */}
            <div className="bg-gray-50 dark:bg-gray-800/50 border border-gray-100 dark:border-gray-700 rounded-lg p-4 mb-4">
              <div className="grid grid-cols-1 md:grid-cols-12 gap-3">
                <div className="md:col-span-6">
                  <label className={labelStyle}>Ürün Seç</label>
                  <select 
                    id="product-select"
                    value={currentItem.productId} 
                    onChange={(e) => handleProductChange(e.target.value)} 
                    className={inputStyle}
                  >
                    <option value="">Ürün Seçin</option>
                    {products.map(p => (
                      <option key={p.id} value={p.id}>{p.name} — {p.price} {p.currency} — Stok: {p.stockQuantity}</option>
                    ))}
                  </select>
                </div>
                <div className="md:col-span-2">
                  <label className={labelStyle}>Adet</label>
                  <input type="number" min="1" value={currentItem.quantity} onChange={(e) => setCurrentItem({ ...currentItem, quantity: parseInt(e.target.value) || 1 })} className={inputStyle} />
                </div>
                <div className="md:col-span-2">
                  <label className={labelStyle}>Birim Fiyat ({formData.currency})</label>
                  <input type="number" step="0.01" min="0" value={currentItem.unitPrice} onChange={(e) => setCurrentItem({ ...currentItem, unitPrice: parseFloat(e.target.value) || 0 })} className={inputStyle} />
                </div>
                <div className="md:col-span-2">
                  <button 
                    type="button" 
                    onClick={handleAddOrUpdateItem} 
                    className="w-full mt-6 px-3 py-2 bg-indigo-600 hover:bg-indigo-700 text-white font-medium rounded-lg text-sm transition flex items-center justify-center gap-1"
                  >
                    <span>{currentItem.editingIndex !== null ? '🔄' : '+'}</span> 
                    {currentItem.editingIndex !== null ? 'Güncelle' : 'Ekle'}
                  </button>
                </div>
              </div>
              {currentItem.editingIndex !== null && (
                <div className="mt-3 text-xs text-amber-700 dark:text-amber-400 bg-amber-50 dark:bg-amber-950/40 px-3 py-1.5 rounded-lg inline-block">
                  ✏️ Düzenleme modu: Mevcut ürünü güncelliyorsunuz. İptal için sayfayı yenileyin.
                </div>
              )}
              {currentItem.productId && currentItem.productCurrency !== formData.currency && !ratesLoading && (
                <div className="mt-3 text-xs text-indigo-700 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-950/40 px-3 py-1.5 rounded-lg inline-block">
                  💱 {currentItem.productCurrency} → {formData.currency} dönüşümü uygulanacak
                </div>
              )}
            </div>

            {/* Ürün Tablosu */}
            {formData.items.length > 0 ? (
              <div className="overflow-x-auto border border-gray-200 dark:border-gray-700 rounded-lg">
                <table className="min-w-full text-sm">
                  <thead className="bg-gray-100 dark:bg-gray-800">
                    <tr className="text-left text-gray-700 dark:text-gray-300">
                      <th className="px-4 py-2 font-medium">Ürün</th>
                      <th className="px-4 py-2 text-right font-medium">Adet</th>
                      <th className="px-4 py-2 text-right font-medium">Birim Fiyat ({formData.currency})</th>
                      <th className="px-4 py-2 text-right font-medium">Toplam</th>
                      <th className="px-4 py-2 text-center w-20">İşlem</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-800">
                    {formData.items.map((item, index) => (
                      <tr key={index} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-4 py-2 text-gray-900 dark:text-gray-100">
                          {item.productName}
                          <span className="ml-1 text-xs text-gray-500 dark:text-gray-400">({item.productCurrency})</span>
                        </td>
                        <td className="px-4 py-2 text-right text-gray-900 dark:text-gray-100 font-medium">{item.quantity}</td>
                        <td className="px-4 py-2 text-right text-gray-900 dark:text-gray-100 font-mono">{formatPrice(item.unitPrice, formData.currency)}</td>
                        <td className="px-4 py-2 text-right text-indigo-700 dark:text-indigo-400 font-bold">{formatPrice(item.totalPrice, formData.currency)}</td>
                        <td className="px-4 py-2 text-center">
                          <div className="flex justify-center gap-2">
                            <button 
                              type="button" 
                              onClick={() => handleEditItem(index)} 
                              className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition"
                              title="Düzenle"
                            >
                              ✏️
                            </button>
                            <button 
                              type="button" 
                              onClick={() => handleRemoveItem(index)} 
                              className="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300 transition"
                              title="Sil"
                            >
                              🗑️
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot className="bg-gray-100 dark:bg-gray-800/50">
                    <tr className="border-t border-gray-200 dark:border-gray-700">
                      <td colSpan={3} className="px-4 py-2 text-right font-medium text-gray-700 dark:text-gray-300">Alt Toplam</td>
                      <td className="px-4 py-2 text-right font-semibold text-gray-900 dark:text-gray-100">{formatPrice(calculateSubTotal(), formData.currency)}</td>
                      <td></td>
                    </tr>
                    <tr>
                      <td colSpan={3} className="px-4 py-1 text-right text-sm text-gray-600 dark:text-gray-400">KDV (%{formData.taxRate})</td>
                      <td className="px-4 py-1 text-right text-gray-900 dark:text-gray-100">{formatPrice(calculateTax(), formData.currency)}</td>
                      <td></td>
                    </tr>
                    <tr className="bg-indigo-50 dark:bg-indigo-950/30">
                      <td colSpan={3} className="px-4 py-2 text-right font-bold text-gray-900 dark:text-gray-100">GENEL TOPLAM</td>
                      <td className="px-4 py-2 text-right font-bold text-indigo-700 dark:text-indigo-400 text-lg">{formatPrice(calculateTotal(), formData.currency)}</td>
                      <td></td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            ) : (
              <div className="text-center py-8 border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-lg">
                <span className="text-3xl mb-1 block">📦</span>
                <p className="text-sm text-gray-500 dark:text-gray-400">Henüz ürün eklenmemiş</p>
              </div>
            )}
          </div>

          {/* Buttons */}
          <div className="flex justify-end gap-3 pt-2">
            <Link 
              to="/orders"   
              className="px-4 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-200 font-medium rounded-lg text-sm hover:bg-gray-100 dark:hover:bg-gray-700 transition"
            >
              İptal
            </Link>
            <button 
              type="submit" 
              disabled={loading || formData.items.length === 0 || !formData.customerId} 
              className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 dark:disabled:bg-indigo-800 text-white font-semibold rounded-lg text-sm transition disabled:cursor-not-allowed flex items-center gap-1"
            >
              {loading ? 'Güncelleniyor...' : 'Değişiklikleri Kaydet'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}