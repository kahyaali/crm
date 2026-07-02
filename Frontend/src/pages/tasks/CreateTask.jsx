// src/pages/tasks/CreateTask.jsx
import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function CreateTask() {
    const navigate = useNavigate();
    const { user } = useAuth();
    const [loading, setLoading] = useState(false);
    const [personels, setPersonels] = useState([]);
    const [customers, setCustomers] = useState([]);
    const [leads, setLeads] = useState([]);
    const [opportunities, setOpportunities] = useState([]);

    const [formData, setFormData] = useState({
        title: '',
        description: '',
        assignedToPersonelId: '',
        relatedToCustomerId: '',
        relatedToLeadId: '',
        relatedToOpportunityId: '',
        status: 'Yeni',
        priority: 'Orta',
        dueDate: ''
    });

    const inputStyle = "w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200";
    const labelStyle = "text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block";
    const selectStyle = "w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200";

    const statusOptions = [
        { value: 'Yeni', label: '🆕 Yeni' },
        { value: 'Devam Ediyor', label: '🔄 Devam Ediyor' },
        { value: 'Tamamlandı', label: '✅ Tamamlandı' },
        { value: 'İptal', label: '🚫 İptal' }
    ];

    const priorityOptions = [
        { value: 'Düşük', label: '🟢 Düşük' },
        { value: 'Orta', label: '🟡 Orta' },
        { value: 'Yüksek', label: '🟠 Yüksek' },
        { value: 'Acil', label: '🔴 Acil' }
    ];

    useEffect(() => {
        fetchPersonels();
        fetchCustomers();
        fetchLeads();
        fetchOpportunities();
    }, []);

    const fetchPersonels = async () => {
        try {
            const response = await api.get('/tasks/personels');
            setPersonels(response.data || []);
        } catch (error) {
            toast.error('Personeller yüklenemedi');
        }
    };

    const fetchCustomers = async () => {
        try {
            const response = await api.get('/tasks/customers');
            setCustomers(response.data || []);
        } catch (error) {
            toast.error('Müşteriler yüklenemedi');
        }
    };

    const fetchLeads = async () => {
        try {
            const response = await api.get('/tasks/leads');
            setLeads(response.data || []);
        } catch (error) {
            toast.error('Leadler yüklenemedi');
        }
    };

    const fetchOpportunities = async () => {
        try {
            const response = await api.get('/tasks/opportunities');
            setOpportunities(response.data || []);
        } catch (error) {
            toast.error('Fırsatlar yüklenemedi');
        }
    };

 const handleSubmit = async (e) => {
    e.preventDefault();

    if (!formData.title.trim()) {
        toast.error('Görev başlığı zorunludur');
        return;
    }

    // Tarihi doğru formata çevir
    let dueDate = null;
    if (formData.dueDate) {
        const dateObj = new Date(formData.dueDate);
        dueDate = dateObj.toISOString(); // UTC format
    }

    const submitData = {
        title: formData.title.trim(),
        description: formData.description?.trim() || null,
        assignedToPersonelId: formData.assignedToPersonelId ? Number(formData.assignedToPersonelId) : null,
        relatedToCustomerId: formData.relatedToCustomerId ? Number(formData.relatedToCustomerId) : null,
        relatedToLeadId: formData.relatedToLeadId ? Number(formData.relatedToLeadId) : null,
        relatedToOpportunityId: formData.relatedToOpportunityId ? Number(formData.relatedToOpportunityId) : null,
        status: formData.status || "Yeni",
        priority: formData.priority || "Orta",
        dueDate: dueDate
    };

    // 🔍 Debug için log
    console.log('📤 Gönderilen veri:', JSON.stringify(submitData, null, 2));

    setLoading(true);
    try {
        const response = await api.post('/tasks', submitData);
        console.log('✅ Başarılı:', response.data);
        toast.success('✅ Görev başarıyla oluşturuldu');
        navigate('/tasks');
    } catch (error) {
        console.error('❌ Hata detayı:', {
            status: error.response?.status,
            data: error.response?.data,
            config: error.config
        });
        
        // Detaylı hata mesajı
        if (error.response?.data?.errors) {
            const errors = error.response.data.errors;
            // FluentValidation hatalarını göster
            if (typeof errors === 'object') {
                Object.values(errors).flat().forEach(err => {
                    toast.error(err);
                });
            } else {
                toast.error(errors);
            }
        } else if (error.response?.data?.message) {
            toast.error(error.response.data.message);
        } else {
            toast.error('Görev oluşturulamadı. Lütfen tüm alanları kontrol edin.');
        }
    } finally {
        setLoading(false);
    }
};

    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Yeni Görev</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400">Yeni görev oluşturun</p>
                </div>
                <Link to="/tasks" className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition">
                    ← Görevlere Dön
                </Link>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden">
                    <div className="px-5 py-3 bg-gray-50 dark:bg-gray-700/50 border-b border-gray-200 dark:border-gray-700">
                        <div className="flex items-center gap-2">
                            <span className="text-base">✅</span>
                            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">Görev Bilgileri</h2>
                        </div>
                    </div>

                    <div className="p-5 space-y-4">
                        <div>
                            <label className={labelStyle}>Görev Başlığı <span className="text-red-500">*</span></label>
                            <input
                                type="text"
                                value={formData.title}
                                onChange={(e) => setFormData({...formData, title: e.target.value})}
                                className={inputStyle}
                                placeholder="Görev başlığı"
                                required
                            />
                        </div>

                        <div>
                            <label className={labelStyle}>Açıklama</label>
                            <textarea
                                rows="3"
                                value={formData.description}
                                onChange={(e) => setFormData({...formData, description: e.target.value})}
                                className={`${inputStyle} resize-none`}
                                placeholder="Görev açıklaması..."
                            />
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
                            <div>
                                <label className={labelStyle}>Durum</label>
                                <select
                                    value={formData.status}
                                    onChange={(e) => setFormData({...formData, status: e.target.value})}
                                    className={selectStyle}
                                >
                                    {statusOptions.map(opt => (
                                        <option key={opt.value} value={opt.value}>{opt.label}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Öncelik</label>
                                <select
                                    value={formData.priority}
                                    onChange={(e) => setFormData({...formData, priority: e.target.value})}
                                    className={selectStyle}
                                >
                                    {priorityOptions.map(opt => (
                                        <option key={opt.value} value={opt.value}>{opt.label}</option>
                                    ))}
                                </select>
                            </div>
                            <div>
                                <label className={labelStyle}>Bitiş Tarihi</label>
                                <input
                                    type="date"
                                    value={formData.dueDate}
                                    onChange={(e) => setFormData({...formData, dueDate: e.target.value})}
                                    className={inputStyle}
                                />
                            </div>
                        </div>

                        <div>
                            <label className={labelStyle}>İlişkili Kayıt (Opsiyonel)</label>
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                                <select
                                    value={formData.relatedToCustomerId}
                                    onChange={(e) => setFormData({...formData, relatedToCustomerId: e.target.value})}
                                    className={selectStyle}
                                >
                                    <option value="">Müşteri</option>
                                    {customers.map(c => (
                                        <option key={c.id} value={c.id}>{c.firstName} {c.lastName}</option>
                                    ))}
                                </select>
                                <select
                                    value={formData.relatedToLeadId}
                                    onChange={(e) => setFormData({...formData, relatedToLeadId: e.target.value})}
                                    className={selectStyle}
                                >
                                    <option value="">Lead</option>
                                    {leads.map(l => (
                                        <option key={l.id} value={l.id}>{l.companyName}</option>
                                    ))}
                                </select>
                                <select
                                    value={formData.relatedToOpportunityId}
                                    onChange={(e) => setFormData({...formData, relatedToOpportunityId: e.target.value})}
                                    className={selectStyle}
                                >
                                    <option value="">Fırsat</option>
                                    {opportunities.map(o => (
                                        <option key={o.id} value={o.id}>{o.name}</option>
                                    ))}
                                </select>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="flex justify-end gap-3">
                    <Link to="/tasks" className="px-4 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-600 transition-all duration-200 font-medium">
                        İptal
                    </Link>
                    <button
                        type="submit"
                        disabled={loading}
                        className="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 dark:disabled:bg-indigo-800 text-white font-medium rounded-lg text-sm transition-all duration-200 disabled:cursor-not-allowed"
                    >
                        {loading ? 'Oluşturuluyor...' : 'Görev Oluştur'}
                    </button>
                </div>
            </form>
        </div>
    );
}