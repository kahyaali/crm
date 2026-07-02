// src/pages/campaigns/EditCampaign.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function EditCampaign() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [loading, setLoading] = useState(false);
    const [pageLoading, setPageLoading] = useState(true);
    
    const [formData, setFormData] = useState({
        id: Number(id),
        name: '',
        description: '',
        type: '',
        startDate: '',
        endDate: '',
        budget: '',
        actualCost: '',
        targetLeads: '',
        convertedLeads: '',
        status: 'Taslak',
        notes: ''
    });

    const currentPersonelId = user?.personelId;

    const typeOptions = [
        { value: 'Email', label: '📧 Email' },
        { value: 'SMS', label: '📱 SMS' },
        { value: 'Sosyal Medya', label: '📱 Sosyal Medya' },
        { value: 'Radyo', label: '📻 Radyo' },
        { value: 'TV', label: '📺 TV' }
    ];

    const statusOptions = [
        { value: 'Taslak', label: '📝 Taslak' },
        { value: 'Aktif', label: '✅ Aktif' },
        { value: 'Tamamlandı', label: '🏁 Tamamlandı' },
        { value: 'İptal', label: '🚫 İptal' }
    ];

    useEffect(() => {
        fetchCampaign();
    }, [id]);

    const fetchCampaign = async () => {
        try {
            setPageLoading(true);
            const response = await api.get(`/campaigns/${id}`);
            const campaign = response.data;
            
            if (String(campaign.createdByPersonelId) !== String(currentPersonelId)) {
                toast.error('Sadece kampanyayı oluşturan kişi düzenleyebilir.');
                navigate('/campaigns');
                return;
            }
            
            setFormData({
                id: campaign.id,
                name: campaign.name || '',
                description: campaign.description || '',
                type: campaign.type || '',
                startDate: campaign.startDate?.slice(0, 16) || '',
                endDate: campaign.endDate?.slice(0, 16) || '',
                budget: campaign.budget || '',
                actualCost: campaign.actualCost || '',
                targetLeads: campaign.targetLeads || '',
                convertedLeads: campaign.convertedLeads || '',
                status: campaign.status || 'Taslak',
                notes: campaign.notes || ''
            });
        } catch (error) {
            toast.error('Kampanya bilgileri yüklenemedi');
            navigate('/campaigns');
        } finally {
            setPageLoading(false);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        if (!formData.name.trim()) {
            toast.error('Kampanya adı zorunludur');
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
            id: Number(id),
            name: formData.name,
            description: formData.description || null,
            type: formData.type || null,
            startDate: formData.startDate,
            endDate: formData.endDate,
            budget: Number(formData.budget) || 0,
            actualCost: formData.actualCost ? Number(formData.actualCost) : null,
            targetLeads: formData.targetLeads ? Number(formData.targetLeads) : null,
            convertedLeads: formData.convertedLeads ? Number(formData.convertedLeads) : null,
            status: formData.status,
            notes: formData.notes || null
        };

        setLoading(true);
        try {
            await api.put(`/campaigns/${id}`, submitData);
            toast.success('✅ Kampanya başarıyla güncellendi');
            navigate('/campaigns');
        } catch (error) {
            toast.error(error.response?.data?.message || 'Kampanya güncellenemedi');
        } finally {
            setLoading(false);
        }
    };

    if (pageLoading) {
        return (
            <div className="flex justify-center items-center h-64">
                <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Kampanya Düzenle</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400">{formData.name} kampanyasını düzenleyin</p>
                </div>
                <Link to="/campaigns" className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition">
                    ← Kampanyalara Dön
                </Link>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden">
                    <div className="px-5 py-3 bg-gray-50 dark:bg-gray-700/50 border-b border-gray-200 dark:border-gray-700">
                        <div className="flex items-center gap-2">
                            <span className="text-base">📣</span>
                            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">Kampanya Bilgileri</h2>
                        </div>
                    </div>
                    
                    <div className="p-5 space-y-4">
                        <div>
                            <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Kampanya Adı <span className="text-red-500">*</span></label>
                            <input 
                                type="text" 
                                value={formData.name} 
                                onChange={(e) => setFormData({...formData, name: e.target.value})} 
                                className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                                placeholder="Kampanya adı"
                                required
                            />
                        </div>

                        <div>
                            <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Açıklama</label>
                            <textarea 
                                rows="3" 
                                value={formData.description} 
                                onChange={(e) => setFormData({...formData, description: e.target.value})} 
                                className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200 resize-none"
                                placeholder="Kampanya açıklaması..."
                            />
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Kampanya Tipi</label>
                                <select 
                                    value={formData.type} 
                                    onChange={(e) => setFormData({...formData, type: e.target.value})} 
                                    className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                                >
                                    <option value="">Seçiniz</option>
                                    {typeOptions.map(opt => (
                                        <option key={opt.value} value={opt.value}>{opt.label}</option>
                                    ))}
                                </select>
                            </div>
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Durum</label>
                                <select 
                                    value={formData.status} 
                                    onChange={(e) => setFormData({...formData, status: e.target.value})} 
                                    className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                                >
                                    {statusOptions.map(opt => (
                                        <option key={opt.value} value={opt.value}>{opt.label}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Başlangıç Tarihi <span className="text-red-500">*</span></label>
                                <input 
                                    type="datetime-local" 
                                    value={formData.startDate} 
                                    onChange={(e) => setFormData({...formData, startDate: e.target.value})} 
                                    className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                                    required
                                />
                            </div>
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Bitiş Tarihi <span className="text-red-500">*</span></label>
                                <input 
                                    type="datetime-local" 
                                    value={formData.endDate} 
                                    onChange={(e) => setFormData({...formData, endDate: e.target.value})} 
                                    className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                                    required
                                />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Bütçe (₺)</label>
                                <input 
                                    type="number" 
                                    value={formData.budget} 
                                    onChange={(e) => setFormData({...formData, budget: e.target.value})} 
                                    className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                                    placeholder="0.00"
                                    step="0.01"
                                />
                            </div>
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Gerçekleşen Maliyet (₺)</label>
                                <input 
                                    type="number" 
                                    value={formData.actualCost} 
                                    onChange={(e) => setFormData({...formData, actualCost: e.target.value})} 
                                    className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                                    placeholder="0.00"
                                    step="0.01"
                                />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Hedef Lead Sayısı</label>
                                <input 
                                    type="number" 
                                    value={formData.targetLeads} 
                                    onChange={(e) => setFormData({...formData, targetLeads: e.target.value})} 
                                    className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                                    placeholder="0"
                                />
                            </div>
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Dönüşen Lead Sayısı</label>
                                <input 
                                    type="number" 
                                    value={formData.convertedLeads} 
                                    onChange={(e) => setFormData({...formData, convertedLeads: e.target.value})} 
                                    className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                                    placeholder="0"
                                />
                            </div>
                        </div>

                        <div>
                            <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block">Notlar</label>
                            <input 
                                type="text" 
                                value={formData.notes} 
                                onChange={(e) => setFormData({...formData, notes: e.target.value})} 
                                className="w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                                placeholder="Notlar..."
                            />
                        </div>
                    </div>
                </div>

                <div className="flex justify-end gap-3">
                    <Link to="/campaigns" className="px-4 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-600 transition-all duration-200 font-medium">
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
        </div>
    );
}