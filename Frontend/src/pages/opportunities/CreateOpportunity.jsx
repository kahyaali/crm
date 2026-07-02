// src/pages/opportunities/CreateOpportunity.jsx
import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function CreateOpportunity() {
    const navigate = useNavigate();
    const { user } = useAuth();
    const [loading, setLoading] = useState(false);
    const [customers, setCustomers] = useState([]);
    const [personels, setPersonels] = useState([]);

    const [formData, setFormData] = useState({
        name: '',
        customerId: '',
        amount: '',
        stage: 'Prospekt',
        assignedToPersonelId: '',
        expectedCloseDate: '',
        description: ''
    });

    const inputStyle = "w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200";
    const labelStyle = "text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block";
    const selectStyle = "w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200";

    const stageOptions = [
        { value: 'Prospekt', label: '🎯 Prospekt' },
        { value: 'Teklif', label: '📄 Teklif' },
        { value: 'Pazarlık', label: '🤝 Pazarlık' },
        { value: 'Kapandı-Kazandı', label: '✅ Kazanıldı' },
        { value: 'Kapandı-Kaybetti', label: '❌ Kaybedildi' }
    ];

    useEffect(() => {
        fetchCustomers();
        fetchPersonels();
    }, []);

    const fetchCustomers = async () => {
        try {
            const response = await api.get('/opportunities/customers');
            setCustomers(response.data || []);
        } catch (error) {
            toast.error('Müşteriler yüklenemedi');
        }
    };

    const fetchPersonels = async () => {
        try {
            const response = await api.get('/opportunities/personels');
            setPersonels(response.data || []);
        } catch (error) {
            toast.error('Personeller yüklenemedi');
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (!formData.name.trim()) {
            toast.error('Fırsat adı zorunludur');
            return;
        }
        if (!formData.customerId) {
            toast.error('Müşteri seçmelisiniz');
            return;
        }

        const submitData = {
            name: formData.name,
            customerId: Number(formData.customerId),
            amount: Number(formData.amount) || 0,
            stage: formData.stage,
            assignedToPersonelId: formData.assignedToPersonelId ? Number(formData.assignedToPersonelId) : null,
            expectedCloseDate: formData.expectedCloseDate || null,
            description: formData.description || null
        };

        setLoading(true);
        try {
            await api.post('/opportunities', submitData);
            toast.success('✅ Fırsat başarıyla oluşturuldu');
            navigate('/opportunities');
        } catch (error) {
            toast.error(error.response?.data?.message || 'Fırsat oluşturulamadı');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Yeni Fırsat</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400">Yeni satış fırsatı oluşturun</p>
                </div>
                <Link to="/opportunities" className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition">
                    ← Fırsatlara Dön
                </Link>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden">
                    <div className="px-5 py-3 bg-gray-50 dark:bg-gray-700/50 border-b border-gray-200 dark:border-gray-700">
                        <div className="flex items-center gap-2">
                            <span className="text-base">🎯</span>
                            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">Fırsat Bilgileri</h2>
                        </div>
                    </div>

                    <div className="p-5 space-y-4">
                        <div>
                            <label className={labelStyle}>Fırsat Adı <span className="text-red-500">*</span></label>
                            <input
                                type="text"
                                value={formData.name}
                                onChange={(e) => setFormData({...formData, name: e.target.value})}
                                className={inputStyle}
                                placeholder="Fırsat adı"
                                required
                            />
                        </div>

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
                                <label className={labelStyle}>Tutar (₺)</label>
                                <input
                                    type="number"
                                    value={formData.amount}
                                    onChange={(e) => setFormData({...formData, amount: e.target.value})}
                                    className={inputStyle}
                                    placeholder="0.00"
                                    step="0.01"
                                />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Aşama</label>
                                <select
                                    value={formData.stage}
                                    onChange={(e) => setFormData({...formData, stage: e.target.value})}
                                    className={selectStyle}
                                >
                                    {stageOptions.map(opt => (
                                        <option key={opt.value} value={opt.value}>{opt.label}</option>
                                    ))}
                                </select>
                            </div>
                            <div>
                                <label className={labelStyle}>Atanan Personel</label>
                                <select
                                    value={formData.assignedToPersonelId}
                                    onChange={(e) => setFormData({...formData, assignedToPersonelId: e.target.value})}
                                    className={selectStyle}
                                >
                                    <option value="">Seçiniz</option>
                                    {personels.map(p => (
                                        <option key={p.id} value={p.id}>{p.firstName} {p.lastName}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        <div>
                            <label className={labelStyle}>Beklenen Kapanış Tarihi</label>
                            <input
                                type="date"
                                value={formData.expectedCloseDate}
                                onChange={(e) => setFormData({...formData, expectedCloseDate: e.target.value})}
                                className={inputStyle}
                            />
                        </div>

                        <div>
                            <label className={labelStyle}>Açıklama</label>
                            <textarea
                                rows="3"
                                value={formData.description}
                                onChange={(e) => setFormData({...formData, description: e.target.value})}
                                className={`${inputStyle} resize-none`}
                                placeholder="Fırsat hakkında detaylı bilgi..."
                            />
                        </div>
                    </div>
                </div>

                <div className="flex justify-end gap-3">
                    <Link to="/opportunities" className="px-4 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-600 transition-all duration-200 font-medium">
                        İptal
                    </Link>
                    <button
                        type="submit"
                        disabled={loading}
                        className="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 dark:disabled:bg-indigo-800 text-white font-medium rounded-lg text-sm transition-all duration-200 disabled:cursor-not-allowed"
                    >
                        {loading ? 'Oluşturuluyor...' : 'Fırsat Oluştur'}
                    </button>
                </div>
            </form>
        </div>
    );
}