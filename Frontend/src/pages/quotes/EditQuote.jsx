// src/pages/quotes/EditQuote.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function EditQuote() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [loading, setLoading] = useState(false);
    const [pageLoading, setPageLoading] = useState(true);
    const [customers, setCustomers] = useState([]);
    const [products, setProducts] = useState([]);
    const [opportunities, setOpportunities] = useState([]);
    
    // Ürün modal states
    const [showProductModal, setShowProductModal] = useState(false);
    const [selectedProduct, setSelectedProduct] = useState(null);
    const [productQuantity, setProductQuantity] = useState(1);
    const [productSearch, setProductSearch] = useState('');
    const [productDescription, setProductDescription] = useState('');
    
    const [formData, setFormData] = useState({
        id: Number(id),
        customerId: '',
        opportunityId: '',
        quoteDate: '',
        validUntil: '',
        taxRate: 20,
        status: 'Taslak',
        notes: '',
        items: []
    });

    const currentPersonelId = user?.personelId;

    useEffect(() => {
        fetchQuote();
        fetchCustomers();
        fetchProducts();
        fetchOpportunities();
    }, [id]);

    const inputStyle = "w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200";
    const labelStyle = "text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block";
    const selectStyle = "w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200";

    const fetchQuote = async () => {
        try {
            setPageLoading(true);
            const response = await api.get(`/quotes/${id}`);
            const quote = response.data;
            
            if (String(quote.createdByPersonelId) !== String(currentPersonelId)) {
                toast.error('Sadece teklifi oluşturan kişi düzenleyebilir.');
                navigate('/quotes');
                return;
            }
            
            setFormData({
                id: quote.id,
                customerId: quote.customerId,
                opportunityId: quote.opportunityId || '',
                quoteDate: quote.quoteDate?.slice(0, 16) || '',
                validUntil: quote.validUntil?.slice(0, 16) || '',
                taxRate: quote.taxRate || 20,
                status: quote.status || 'Taslak',
                notes: quote.notes || '',
                items: quote.items?.map(item => ({
                    id: item.id,
                    productId: item.productId,
                    productName: item.productName,
                    productCode: item.productCode,
                    quantity: item.quantity,
                    unitPrice: item.unitPrice,
                    totalPrice: item.totalPrice,
                    description: item.description || ''
                })) || []
            });
        } catch (error) {
            toast.error('Teklif bilgileri yüklenemedi');
            navigate('/quotes');
        } finally {
            setPageLoading(false);
        }
    };

    const fetchCustomers = async () => {
        try {
            const response = await api.get('/quotes/customers');
            setCustomers(response.data || []);
        } catch (error) {
            toast.error('Müşteriler yüklenemedi');
        }
    };

    const fetchProducts = async () => {
        try {
            const response = await api.get('/quotes/products');
            setProducts(response.data || []);
        } catch (error) {
            toast.error('Ürünler yüklenemedi');
        }
    };

    const fetchOpportunities = async () => {
        try {
            const response = await api.get('/quotes/opportunities');
            setOpportunities(response.data || []);
        } catch (error) {
            toast.error('Fırsatlar yüklenemedi');
        }
    };

    const handleAddProduct = () => {
        if (!selectedProduct) return;
        if (productQuantity <= 0) {
            toast.error('Geçerli bir adet giriniz');
            return;
        }

        const existingIndex = formData.items.findIndex(i => i.productId === selectedProduct.id);
        
        if (existingIndex >= 0) {
            const newItems = [...formData.items];
            newItems[existingIndex].quantity += productQuantity;
            newItems[existingIndex].totalPrice = newItems[existingIndex].quantity * newItems[existingIndex].unitPrice;
            setFormData(prev => ({ ...prev, items: newItems }));
        } else {
            const newItem = {
                productId: selectedProduct.id,
                productName: selectedProduct.name,
                productCode: selectedProduct.sku,
                quantity: productQuantity,
                unitPrice: selectedProduct.price || 0,
                totalPrice: productQuantity * (selectedProduct.price || 0),
                description: productDescription
            };
            setFormData(prev => ({ ...prev, items: [...prev.items, newItem] }));
        }
        
        setSelectedProduct(null);
        setProductQuantity(1);
        setProductDescription('');
        setShowProductModal(false);
        setProductSearch('');
    };

    const handleRemoveItem = (index) => {
        setFormData(prev => ({
            ...prev,
            items: prev.items.filter((_, i) => i !== index)
        }));
    };

    const updateItemQuantity = (index, newQuantity) => {
        if (newQuantity <= 0) return;
        const newItems = [...formData.items];
        newItems[index].quantity = newQuantity;
        newItems[index].totalPrice = newItems[index].quantity * newItems[index].unitPrice;
        setFormData(prev => ({ ...prev, items: newItems }));
    };

    const updateItemUnitPrice = (index, newPrice) => {
        if (newPrice < 0) return;
        const newItems = [...formData.items];
        newItems[index].unitPrice = newPrice;
        newItems[index].totalPrice = newItems[index].quantity * newPrice;
        setFormData(prev => ({ ...prev, items: newItems }));
    };

    const calculateSubTotal = () => {
        return formData.items.reduce((sum, item) => sum + item.totalPrice, 0);
    };

    const calculateTaxAmount = () => {
        return calculateSubTotal() * (formData.taxRate / 100);
    };

    const calculateTotalAmount = () => {
        return calculateSubTotal() + calculateTaxAmount();
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        if (!formData.customerId) {
            toast.error('Müşteri seçmelisiniz');
            return;
        }
        if (formData.items.length === 0) {
            toast.error('En az bir ürün eklemelisiniz');
            return;
        }

        const submitData = {
            id: Number(id),
            customerId: Number(formData.customerId),
            opportunityId: formData.opportunityId ? Number(formData.opportunityId) : null,
            quoteDate: formData.quoteDate,
            validUntil: formData.validUntil,
            taxRate: formData.taxRate,
            status: formData.status,
            notes: formData.notes,
            items: formData.items.map(item => ({
                id: item.id,
                productId: item.productId,
                quantity: item.quantity,
                unitPrice: item.unitPrice,
                description: item.description || ''
            }))
        };

        setLoading(true);
        try {
            await api.put(`/quotes/${id}`, submitData);
            toast.success('✅ Teklif başarıyla güncellendi');
            navigate('/quotes');
        } catch (error) {
            toast.error(error.response?.data?.message || 'Teklif güncellenemedi');
        } finally {
            setLoading(false);
        }
    };

    const formatCurrency = (amount) => {
        return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(amount || 0);
    };

    if (pageLoading) {
        return (
            <div className="flex justify-center items-center h-64">
                <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 py-8 max-w-5xl">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Teklif Düzenle</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400">{formData.quoteNumber} nolu teklifi düzenleyin</p>
                </div>
                <Link to="/quotes" className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition">
                    ← Tekliflere Dön
                </Link>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden">
                    <div className="px-5 py-3 bg-gray-50 dark:bg-gray-700/50 border-b border-gray-200 dark:border-gray-700">
                        <div className="flex items-center gap-2">
                            <span className="text-base">📄</span>
                            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">Teklif Bilgileri</h2>
                        </div>
                    </div>
                    
                    <div className="p-5 space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Müşteri <span className="text-red-500">*</span></label>
                                <select 
                                    value={formData.customerId} 
                                    onChange={(e) => setFormData({...formData, customerId: e.target.value})} 
                                    className={selectStyle}
                                    required
                                >
                                    <option value="">Müşteri Seçin</option>
                                    {customers.map(c => (
                                        <option key={c.id} value={c.id}>{c.firstName} {c.lastName} {c.companyName ? `(${c.companyName})` : ''}</option>
                                    ))}
                                </select>
                            </div>
                            <div>
                                <label className={labelStyle}>Fırsat</label>
                                <select 
                                    value={formData.opportunityId} 
                                    onChange={(e) => setFormData({...formData, opportunityId: e.target.value})} 
                                    className={selectStyle}
                                >
                                    <option value="">Fırsat Seçin</option>
                                    {opportunities.map(o => (
                                        <option key={o.id} value={o.id}>{o.name}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Teklif Tarihi</label>
                                <input 
                                    type="datetime-local" 
                                    value={formData.quoteDate} 
                                    onChange={(e) => setFormData({...formData, quoteDate: e.target.value})} 
                                    className={inputStyle}
                                />
                            </div>
                            <div>
                                <label className={labelStyle}>Geçerlilik Tarihi</label>
                                <input 
                                    type="datetime-local" 
                                    value={formData.validUntil} 
                                    onChange={(e) => setFormData({...formData, validUntil: e.target.value})} 
                                    className={inputStyle}
                                />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>KDV Oranı (%)</label>
                                <input 
                                    type="number" 
                                    value={formData.taxRate} 
                                    onChange={(e) => setFormData({...formData, taxRate: Number(e.target.value)})} 
                                    className={inputStyle}
                                />
                            </div>
                            <div>
                                <label className={labelStyle}>Durum</label>
                                <select 
                                    value={formData.status} 
                                    onChange={(e) => setFormData({...formData, status: e.target.value})} 
                                    className={selectStyle}
                                >
                                    <option value="Taslak">📝 Taslak</option>
                                    <option value="Gönderildi">📨 Gönderildi</option>
                                    <option value="Onaylandı">✅ Onaylandı</option>
                                    <option value="Reddedildi">❌ Reddedildi</option>
                                    <option value="İptal">🚫 İptal</option>
                                </select>
                            </div>
                        </div>

                        <div>
                            <label className={labelStyle}>Notlar</label>
                            <input 
                                type="text" 
                                value={formData.notes} 
                                onChange={(e) => setFormData({...formData, notes: e.target.value})} 
                                className={inputStyle}
                                placeholder="Teklif notu..."
                            />
                        </div>

                        {/* Ürünler */}
                        <div>
                            <label className={labelStyle}>Teklif Kalemleri</label>
                            
                            <div className="mb-3 p-3 bg-gray-50 dark:bg-gray-800/30 rounded-lg">
                                <table className="w-full text-sm">
                                    <thead>
                                        <tr className="border-b border-gray-200 dark:border-gray-700">
                                            <th className="text-left py-2 text-gray-600 dark:text-gray-400">Ürün</th>
                                            <th className="text-center py-2 text-gray-600 dark:text-gray-400">Adet</th>
                                            <th className="text-right py-2 text-gray-600 dark:text-gray-400">Birim Fiyat</th>
                                            <th className="text-right py-2 text-gray-600 dark:text-gray-400">Toplam</th>
                                            <th className="text-center py-2"></th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {formData.items.length === 0 ? (
                                            <tr>
                                                <td colSpan={5} className="text-center py-4 text-gray-400 dark:text-gray-500">
                                                    Henüz ürün eklenmedi
                                                </td>
                                            </tr>
                                        ) : (
                                            formData.items.map((item, index) => (
                                                <tr key={index} className="border-b border-gray-100 dark:border-gray-700">
                                                    <td className="py-2">
                                                        <div>
                                                            <div className="font-medium text-gray-900 dark:text-white">{item.productName}</div>
                                                            <div className="text-xs text-gray-400 dark:text-gray-500">{item.productCode}</div>
                                                            {item.description && <div className="text-xs text-gray-500 dark:text-gray-400">{item.description}</div>}
                                                        </div>
                                                    </td>
                                                    <td className="py-2 text-center">
                                                        <input 
                                                            type="number" 
                                                            value={item.quantity} 
                                                            onChange={(e) => updateItemQuantity(index, Number(e.target.value))}
                                                            className="w-20 px-2 py-1 text-center border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                                                            min="1"
                                                        />
                                                    </td>
                                                    <td className="py-2 text-right">
                                                        <input 
                                                            type="number" 
                                                            value={item.unitPrice} 
                                                            onChange={(e) => updateItemUnitPrice(index, Number(e.target.value))}
                                                            className="w-28 px-2 py-1 text-right border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                                                            step="0.01"
                                                        />
                                                    </td>
                                                    <td className="py-2 text-right font-medium text-gray-900 dark:text-white">
                                                        {formatCurrency(item.totalPrice)}
                                                    </td>
                                                    <td className="py-2 text-center">
                                                        <button 
                                                            type="button"
                                                            onClick={() => handleRemoveItem(index)}
                                                            className="text-red-500 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300"
                                                        >
                                                            ✕
                                                        </button>
                                                    </td>
                                                </tr>
                                            ))
                                        )}
                                    </tbody>
                                </table>
                            </div>

                            <button
                                type="button"
                                onClick={() => setShowProductModal(true)}
                                className="w-full py-2.5 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg text-gray-500 dark:text-gray-400 hover:border-indigo-500 hover:text-indigo-600 dark:hover:border-indigo-400 dark:hover:text-indigo-400 transition-colors flex items-center justify-center gap-2 text-sm font-medium"
                            >
                                + Ürün Ekle
                            </button>
                        </div>

                        {/* Toplam Bilgileri */}
                        <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
                            <div className="flex justify-end">
                                <div className="w-80 space-y-2">
                                    <div className="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                                        <span>Ara Toplam:</span>
                                        <span>{formatCurrency(calculateSubTotal())}</span>
                                    </div>
                                    <div className="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                                        <span>KDV (%{formData.taxRate}):</span>
                                        <span>{formatCurrency(calculateTaxAmount())}</span>
                                    </div>
                                    <div className="flex justify-between text-lg font-bold border-t border-gray-200 dark:border-gray-700 pt-2">
                                        <span className="text-gray-900 dark:text-white">Genel Toplam:</span>
                                        <span className="text-indigo-600 dark:text-indigo-400">{formatCurrency(calculateTotalAmount())}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="flex justify-end gap-3">
                    <Link to="/quotes" className="px-4 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-600 transition-all duration-200 font-medium">
                        İptal
                    </Link>
                    <button 
                        type="submit" 
                        disabled={loading} 
                        className="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 dark:disabled:bg-indigo-800 text-white font-medium rounded-lg text-sm transition-all duration-200 disabled:cursor-not-allowed"
                    >
                        {loading ? 'Güncelleniyor...' : 'Değişiklikleri Kaydet'}
                    </button>
                </div>
            </form>

            {/* Ürün Seçme Modalı */}
            {showProductModal && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-2xl max-h-[80vh] flex flex-col">
                        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center">
                            <h2 className="text-xl font-bold text-gray-900 dark:text-white">Ürün Seç</h2>
                            <button type="button" onClick={() => setShowProductModal(false)} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
                                ✕
                            </button>
                        </div>
                        <div className="p-4 border-b border-gray-200 dark:border-gray-700">
                            <input 
                                type="text" 
                                placeholder="Ürün ara..." 
                                value={productSearch} 
                                onChange={(e) => setProductSearch(e.target.value)} 
                                className="w-full px-4 py-2 bg-gray-100 dark:bg-gray-700 rounded-lg text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
                            />
                        </div>
                        <div className="flex-1 overflow-y-auto p-4">
                            <div className="space-y-2">
                                {products
                                    .filter(p => p.name.toLowerCase().includes(productSearch.toLowerCase()) || p.sku?.toLowerCase().includes(productSearch.toLowerCase()))
                                    .map(product => (
                                        <div 
                                            key={product.id} 
                                            onClick={() => setSelectedProduct(product)}
                                            className={`p-3 rounded-lg cursor-pointer transition-colors ${selectedProduct?.id === product.id ? 'bg-indigo-50 dark:bg-indigo-900/30 border border-indigo-200 dark:border-indigo-800' : 'hover:bg-gray-50 dark:hover:bg-gray-700/50'}`}
                                        >
                                            <div className="flex justify-between items-center">
                                                <div>
                                                    <p className="font-medium text-gray-900 dark:text-white">{product.name}</p>
                                                    <p className="text-sm text-gray-500 dark:text-gray-400">SKU: {product.sku}</p>
                                                </div>
                                                <div className="text-right">
                                                    <p className="font-semibold text-indigo-600 dark:text-indigo-400">{formatCurrency(product.price || 0)}</p>
                                                    <p className="text-xs text-gray-400 dark:text-gray-500">Stok: {product.stockQuantity || 0}</p>
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                            </div>
                        </div>
                        {selectedProduct && (
                            <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700">
                                <div className="flex items-center gap-4">
                                    <div className="flex-1">
                                        <label className="text-sm font-medium text-gray-700 dark:text-gray-300 block mb-1">Adet</label>
                                        <input 
                                            type="number" 
                                            value={productQuantity} 
                                            onChange={(e) => setProductQuantity(Number(e.target.value))} 
                                            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                                            min="1"
                                        />
                                    </div>
                                    <div className="flex-1">
                                        <label className="text-sm font-medium text-gray-700 dark:text-gray-300 block mb-1">Birim Fiyat</label>
                                        <input 
                                            type="number" 
                                            value={selectedProduct.price || 0} 
                                            onChange={(e) => setSelectedProduct({...selectedProduct, price: Number(e.target.value)})} 
                                            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                                            step="0.01"
                                        />
                                    </div>
                                    <div className="flex-1">
                                        <label className="text-sm font-medium text-gray-700 dark:text-gray-300 block mb-1">Açıklama</label>
                                        <input 
                                            type="text" 
                                            value={productDescription} 
                                            onChange={(e) => setProductDescription(e.target.value)} 
                                            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                                            placeholder="Opsiyonel..."
                                        />
                                    </div>
                                    <div className="flex items-end">
                                        <button onClick={handleAddProduct} className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg transition-colors">
                                            Ekle
                                        </button>
                                    </div>
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
}