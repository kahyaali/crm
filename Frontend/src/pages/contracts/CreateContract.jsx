// src/pages/contracts/CreateContract.jsx
import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function CreateContract() {
    const navigate = useNavigate();
    const { user } = useAuth();
    const [loading, setLoading] = useState(false);
    const [customers, setCustomers] = useState([]);
    
    const [formData, setFormData] = useState({
        customerId: '',
        title: '',
        description: '',
        startDate: new Date().toISOString().slice(0, 16),
        endDate: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString().slice(0, 16),
        contractValue: '',
        status: 'Taslak',
        notes: '',
        quoteId: ''
    });

    useEffect(() => {
        fetchCustomers();
    }, []);

    const fetchCustomers = async () => {
        try {
            const response = await api.get('/contracts/customers');
            setCustomers(response.data || []);
        } catch (error) {
            toast.error('Müşteriler yüklenemedi');
        }
    };

    const inputStyle = "w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200";
    const labelStyle = "text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block";
    const selectStyle = "w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200";

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        if (!formData.customerId) {
            toast.error('Müşteri seçmelisiniz');
            return;
        }
        if (!formData.title.trim()) {
            toast.error('Sözleşme başlığı zorunludur');
            return;
        }
        if (!formData.startDate) {
            toast.error('Başlangıç tarihi zorunludur');
            return;
        }
        if (!formData.endDate) {
            toast.error('Bitiş tarihi zorunludur');
            return;
        }
        if (new Date(formData.startDate) >= new Date(formData.endDate)) {
            toast.error('Bitiş tarihi, başlangıç tarihinden sonra olmalıdır');
            return;
        }

        const submitData = {
            customerId: Number(formData.customerId),
            title: formData.title,
            description: formData.description || null,
            startDate: formData.startDate,
            endDate: formData.endDate,
            contractValue: Number(formData.contractValue) || 0,
            status: formData.status,
            notes: formData.notes || null,
            quoteId: formData.quoteId ? Number(formData.quoteId) : null
        };

        setLoading(true);
        try {
            await api.post('/contracts', submitData);
            toast.success('✅ Sözleşme başarıyla oluşturuldu');
            navigate('/contracts');
        } catch (error) {
            toast.error(error.response?.data?.message || 'Sözleşme oluşturulamadı');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Yeni Sözleşme</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400">Yeni sözleşme oluşturun</p>
                </div>
                <Link to="/contracts" className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition">
                    ← Sözleşmelere Dön
                </Link>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden">
                    <div className="px-5 py-3 bg-gray-50 dark:bg-gray-700/50 border-b border-gray-200 dark:border-gray-700">
                        <div className="flex items-center gap-2">
                            <span className="text-base">📄</span>
                            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">Sözleşme Bilgileri</h2>
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
                                <label className={labelStyle}>Başlık <span className="text-red-500">*</span></label>
                                <input 
                                    type="text" 
                                    value={formData.title} 
                                    onChange={(e) => setFormData({...formData, title: e.target.value})} 
                                    className={inputStyle}
                                    placeholder="Sözleşme başlığı"
                                    required
                                />
                            </div>
                        </div>

                        <div>
                            <label className={labelStyle}>Açıklama</label>
                            <textarea 
                                rows="3" 
                                value={formData.description} 
                                onChange={(e) => setFormData({...formData, description: e.target.value})} 
                                className={`${inputStyle} resize-none`}
                                placeholder="Sözleşme açıklaması..."
                            />
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Başlangıç Tarihi <span className="text-red-500">*</span></label>
                                <input 
                                    type="datetime-local" 
                                    value={formData.startDate} 
                                    onChange={(e) => setFormData({...formData, startDate: e.target.value})} 
                                    className={inputStyle}
                                    required
                                />
                            </div>
                            <div>
                                <label className={labelStyle}>Bitiş Tarihi <span className="text-red-500">*</span></label>
                                <input 
                                    type="datetime-local" 
                                    value={formData.endDate} 
                                    onChange={(e) => setFormData({...formData, endDate: e.target.value})} 
                                    className={inputStyle}
                                    required
                                />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Sözleşme Değeri (₺)</label>
                                <input 
                                    type="number" 
                                    value={formData.contractValue} 
                                    onChange={(e) => setFormData({...formData, contractValue: e.target.value})} 
                                    className={inputStyle}
                                    placeholder="0.00"
                                    step="0.01"
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
                                    <option value="Bekliyor">⏳ Bekliyor</option>
                                    <option value="Aktif">✅ Aktif</option>
                                    <option value="Süresi Doldu">⏰ Süresi Doldu</option>
                                    <option value="Feshedildi">🚫 Feshedildi</option>
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
                                placeholder="Notlar..."
                            />
                        </div>
                    </div>
                </div>

                <div className="flex justify-end gap-3">
                    <Link to="/contracts" className="px-4 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-600 transition-all duration-200 font-medium">
                        İptal
                    </Link>
                    <button 
                        type="submit" 
                        disabled={loading} 
                        className="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 dark:disabled:bg-indigo-800 text-white font-medium rounded-lg text-sm transition-all duration-200 disabled:cursor-not-allowed"
                    >
                        {loading ? 'Oluşturuluyor...' : 'Sözleşme Oluştur'}
                    </button>
                </div>
            </form>
        </div>
    );
}