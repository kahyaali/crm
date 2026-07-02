import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import toast from 'react-hot-toast';
import api from '../services/api';

export default function CreateOrder() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);
  const [customers, setCustomers] = useState([]);
  const [products, setProducts] = useState([]);
  
  const [rates, setRates] = useState({ TRY: 1 });
  const [ratesLoading, setRatesLoading] = useState(true);
  
  const [formData, setFormData] = useState({
    customerId: '',
    orderDate: new Date().toISOString().split('T')[0],
    deliveryDate: '',
    shippingAddress: '',
    notes: '',
    currency: 'TRY',
    taxRate: 20,
    items: []
  });

  const [currentItem, setCurrentItem] = useState({
    productId: '',
    quantity: 1,
    unitPrice: 0,
    originalPrice: 0,
    productCurrency: 'TRY'
  });

  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';

  const currencies = [
    { code: 'TRY', symbol: '₺', name: 'Türk Lirası' },
    { code: 'USD', symbol: '$', name: 'Amerikan Doları' },
    { code: 'EUR', symbol: '€', name: 'Euro' },
    { code: 'GBP', symbol: '£', name: 'İngiliz Sterlini' }
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

  const getUnitIcon = (unit) => {
    const icons = {
      'Adet': '📦', 'Paket': '📦', 'Kutu': '📦',
      'Kg': '⚖️', 'Litre': '🧪', 'Metre': '📏'
    };
    return icons[unit] || '📦';
  };

  useEffect(() => {
    if (!isAdmin) {
      toast.error('Sipariş oluşturma yetkiniz yok');
      navigate('/orders');
      return;
    }
    fetchCustomers();
    fetchProducts();
    fetchExchangeRates();
  }, []);

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

  const handleProductChange = (productId) => {
    if (!productId) {
      setCurrentItem({ productId: '', quantity: 1, unitPrice: 0, originalPrice: 0, productCurrency: 'TRY' });
      return;
    }
    const product = products.find(p => p.id === parseInt(productId));
    if (product) {
      const convertedPrice = convertCurrency(product.price, product.currency || 'TRY', formData.currency);
      setCurrentItem({
        productId: productId,
        quantity: 1,
        unitPrice: convertedPrice,
        originalPrice: product.price,
        productCurrency: product.currency || 'TRY'
      });
    }
  };

  const handleAddItem = () => {
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
    const isAlreadyAdded = formData.items.some(item => item.productId === product.id);
    if (isAlreadyAdded) {
      toast.error('Bu ürün zaten listeye eklenmiş.');
      return;
    }

    const newItem = {
      productId: parseInt(currentItem.productId),
      productName: product?.name,
      unit: product?.unit || 'Adet',
      quantity: currentItem.quantity,
      unitPrice: currentItem.unitPrice,
      productCurrency: currentItem.productCurrency,
      originalPrice: currentItem.originalPrice,
      totalPrice: currentItem.quantity * currentItem.unitPrice
    };
    setFormData(prev => ({ ...prev, items: [...prev.items, newItem] }));
    setCurrentItem({ productId: '', quantity: 1, unitPrice: 0, originalPrice: 0, productCurrency: 'TRY' });
    toast.success('Ürün eklendi');
  };

  const handleRemoveItem = (index) => {
    setFormData(prev => ({ ...prev, items: prev.items.filter((_, i) => i !== index) }));
    toast.success('Ürün kaldırıldı');
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
    if (!formData.customerId || formData.items.length === 0) return;

    setLoading(true);
    try {
      const submitData = {
        customerId: parseInt(formData.customerId),
        orderDate: formData.orderDate,
        deliveryDate: formData.deliveryDate || null,
        shippingAddress: formData.shippingAddress || null,
        notes: formData.notes || null,
        currency: formData.currency,
        taxRate: formData.taxRate / 100,
        items: formData.items.map(item => ({
          productId: item.productId,
          quantity: item.quantity,
          unitPrice: item.unitPrice
        }))
      };
      await api.post('/Orders', submitData);
      toast.success('✅ Sipariş başarıyla oluşturuldu');
      navigate('/orders');
    } catch (error) {
      toast.error(error.response?.data?.message || 'Sipariş oluşturulamadı');
    } finally {
      setLoading(false);
    }
  };

  if (!isAdmin) return null;

  const inputStyle = "w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200 text-sm";
  const labelStyle = "text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block";

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950 px-4 py-6">
      <div className="max-w-6xl mx-auto">
        
        {/* Header */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 mb-6 pb-3 border-b border-gray-200 dark:border-gray-800">
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Yeni Sipariş</h1>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
              {ratesLoading ? '🔄 Canlı kurlar yükleniyor...' : '✅ Canlı döviz kurları aktif'}
            </p>
          </div>
          <Link to="/orders" className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition">
            <span>←</span> Siparişlere Dön
          </Link>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          
          {/* Sipariş Bilgileri Kartı */}
          <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm overflow-hidden">
            <div className="px-5 py-3 bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-700">
              <div className="flex items-center gap-2">
                <span className="text-base">📝</span>
                <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">Sipariş Bilgileri</h2>
              </div>
            </div>
            <div className="p-5">
              {/* Üst Satır: Müşteri, Para Birimi, KDV - Yan Yana */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                <div className="md:col-span-1">
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
              </div>

              {/* İkinci Satır: Sipariş Tarihi, Teslimat Tarihi - Yan Yana */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                <div>
                  <label className={labelStyle}>Sipariş Tarihi</label>
                  <input type="date" value={formData.orderDate} onChange={(e) => setFormData({ ...formData, orderDate: e.target.value })} className={inputStyle} />
                </div>
                <div>
                  <label className={labelStyle}>Teslimat Tarihi</label>
                  <input type="date" value={formData.deliveryDate} onChange={(e) => setFormData({ ...formData, deliveryDate: e.target.value })} className={inputStyle} />
                </div>
              </div>

              {/* Üçüncü Satır: Teslimat Adresi */}
              <div className="mb-4">
                <label className={labelStyle}>Teslimat Adresi</label>
                <textarea rows="2" value={formData.shippingAddress} onChange={(e) => setFormData({ ...formData, shippingAddress: e.target.value })} className={`${inputStyle} resize-none`} placeholder="Teslimat adresi..." />
              </div>

              {/* Dördüncü Satır: Sipariş Notları */}
              <div>
                <label className={labelStyle}>Sipariş Notları</label>
                <textarea rows="2" value={formData.notes} onChange={(e) => setFormData({ ...formData, notes: e.target.value })} className={`${inputStyle} resize-none`} placeholder="Sipariş notu..." />
              </div>
            </div>
          </div>

          {/* Ürünler Kartı */}
          <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm overflow-hidden">
            <div className="px-5 py-3 bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-700">
              <div className="flex items-center gap-2">
                <span className="text-base">📦</span>
                <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">Sipariş Kalemleri</h2>
              </div>
            </div>
            <div className="p-5">
              
              {/* Ürün Ekleme Formu */}
              <div className="bg-gray-50 dark:bg-gray-800/30 rounded-lg p-4 mb-5 border border-gray-100 dark:border-gray-700">
                <div className="grid grid-cols-1 md:grid-cols-12 gap-3">
                  <div className="md:col-span-6">
                    <label className={labelStyle}>Ürün Seç</label>
                    <select value={currentItem.productId} onChange={(e) => handleProductChange(e.target.value)} className={inputStyle}>
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
                    <button type="button" onClick={handleAddItem} className="w-full mt-6 px-3 py-2 bg-indigo-600 hover:bg-indigo-700 text-white font-medium rounded-lg text-sm transition flex items-center justify-center gap-1">
                      <span>+</span> Ekle
                    </button>
                  </div>
                </div>
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
                      <tr>
                        <th className="px-4 py-2 text-left text-xs font-medium text-gray-600 dark:text-gray-400 uppercase tracking-wider">Ürün</th>
                        <th className="px-4 py-2 text-right text-xs font-medium text-gray-600 dark:text-gray-400 uppercase tracking-wider">Adet</th>
                        <th className="px-4 py-2 text-right text-xs font-medium text-gray-600 dark:text-gray-400 uppercase tracking-wider">Birim Fiyat</th>
                        <th className="px-4 py-2 text-right text-xs font-medium text-gray-600 dark:text-gray-400 uppercase tracking-wider">Toplam</th>
                        <th className="px-4 py-2 text-center text-xs font-medium text-gray-600 dark:text-gray-400 uppercase tracking-wider">İşlem</th>
                       </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100 dark:divide-gray-800">
                      {formData.items.map((item, index) => (
                        <tr key={index} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                          <td className="px-4 py-2 text-gray-900 dark:text-gray-100">
                            {item.productName}
                            <span className="ml-1 text-xs text-gray-500 dark:text-gray-400">({item.productCurrency})</span>
                          </td>
                          <td className="px-4 py-2 text-right text-gray-900 dark:text-gray-100">
                            {item.quantity} <span className="text-xs text-gray-500 dark:text-gray-400">{item.unit}</span>
                          </td>
                          <td className="px-4 py-2 text-right text-gray-900 dark:text-gray-100 font-mono">{formatPrice(item.unitPrice, formData.currency)}</td>
                          <td className="px-4 py-2 text-right text-indigo-700 dark:text-indigo-400 font-bold">{formatPrice(item.totalPrice, formData.currency)}</td>
                          <td className="px-4 py-2 text-center">
                            <button 
                              type="button" 
                              onClick={() => handleRemoveItem(index)} 
                              className="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300 transition"
                              title="Sil"
                            >
                              🗑️
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot className="bg-gray-100 dark:bg-gray-800/50 border-t border-gray-200 dark:border-gray-700">
                      <tr>
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
              disabled={loading || ratesLoading || formData.items.length === 0 || !formData.customerId}
              className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 dark:disabled:bg-indigo-800 text-white font-semibold rounded-lg text-sm transition disabled:cursor-not-allowed flex items-center gap-1"
            >
              {loading ? 'Oluşturuluyor...' : 'Sipariş Oluştur'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}