// src/pages/campaigns/CreateCampaign.jsx
import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function CreateCampaign() {
    const navigate = useNavigate();
    const { user } = useAuth();
    const [loading, setLoading] = useState(false);
    
    const [formData, setFormData] = useState({
        name: '',
        description: '',
        type: '',
        startDate: new Date().toISOString().slice(0, 16),
        endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 16),
        budget: '',
        actualCost: '',
        targetLeads: '',
        convertedLeads: '',
        status: 'Taslak',
        notes: ''
    });

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

    const inputStyle = "w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200";
    const labelStyle = "text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block";
    const selectStyle = "w-full px-3 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200";

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
            await api.post('/campaigns', submitData);
            toast.success('✅ Kampanya başarıyla oluşturuldu');
            navigate('/campaigns');
        } catch (error) {
            toast.error(error.response?.data?.message || 'Kampanya oluşturulamadı');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Yeni Kampanya</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400">Yeni kampanya oluşturun</p>
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
                            <label className={labelStyle}>Kampanya Adı <span className="text-red-500">*</span></label>
                            <input 
                                type="text" 
                                value={formData.name} 
                                onChange={(e) => setFormData({...formData, name: e.target.value})} 
                                className={inputStyle}
                                placeholder="Kampanya adı"
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
                                placeholder="Kampanya açıklaması..."
                            />
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Kampanya Tipi</label>
                                <select 
                                    value={formData.type} 
                                    onChange={(e) => setFormData({...formData, type: e.target.value})} 
                                    className={selectStyle}
                                >
                                    <option value="">Seçiniz</option>
                                    {typeOptions.map(opt => (
                                        <option key={opt.value} value={opt.value}>{opt.label}</option>
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
                                <label className={labelStyle}>Bütçe (₺)</label>
                                <input 
                                    type="number" 
                                    value={formData.budget} 
                                    onChange={(e) => setFormData({...formData, budget: e.target.value})} 
                                    className={inputStyle}
                                    placeholder="0.00"
                                    step="0.01"
                                />
                            </div>
                            <div>
                                <label className={labelStyle}>Gerçekleşen Maliyet (₺)</label>
                                <input 
                                    type="number" 
                                    value={formData.actualCost} 
                                    onChange={(e) => setFormData({...formData, actualCost: e.target.value})} 
                                    className={inputStyle}
                                    placeholder="0.00"
                                    step="0.01"
                                />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Hedef Lead Sayısı</label>
                                <input 
                                    type="number" 
                                    value={formData.targetLeads} 
                                    onChange={(e) => setFormData({...formData, targetLeads: e.target.value})} 
                                    className={inputStyle}
                                    placeholder="0"
                                />
                            </div>
                            <div>
                                <label className={labelStyle}>Dönüşen Lead Sayısı</label>
                                <input 
                                    type="number" 
                                    value={formData.convertedLeads} 
                                    onChange={(e) => setFormData({...formData, convertedLeads: e.target.value})} 
                                    className={inputStyle}
                                    placeholder="0"
                                />
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
                    <Link to="/campaigns" className="px-4 py-2.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-600 transition-all duration-200 font-medium">
                        İptal
                    </Link>
                    <button 
                        type="submit" 
                        disabled={loading} 
                        className="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 dark:disabled:bg-indigo-800 text-white font-medium rounded-lg text-sm transition-all duration-200 disabled:cursor-not-allowed"
                    >
                        {loading ? 'Oluşturuluyor...' : 'Kampanya Oluştur'}
                    </button>
                </div>
            </form>
        </div>
    );
}