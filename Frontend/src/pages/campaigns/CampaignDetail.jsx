// src/pages/campaigns/CampaignDetail.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function CampaignDetail() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [campaign, setCampaign] = useState(null);
    const [loading, setLoading] = useState(true);
    const [updating, setUpdating] = useState(false);

    const isCreator = campaign?.createdByPersonelId === user?.personelId;
    const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
    const canEdit = isAdmin || isCreator;

    useEffect(() => {
        fetchCampaign();
    }, [id]);

    const fetchCampaign = async () => {
        try {
            setLoading(true);
            const response = await api.get(`/campaigns/${id}`);
            setCampaign(response.data);
        } catch (error) {
            toast.error('Kampanya detayı yüklenemedi');
            navigate('/campaigns');
        } finally {
            setLoading(false);
        }
    };

    const formatDate = (date) => {
        if (!date) return '-';
        return new Date(date).toLocaleString('tr-TR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    const formatCurrency = (amount) => {
        return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(amount || 0);
    };

    const getStatusBadge = (status) => {
        const config = {
            'Taslak': { icon: '📝', text: 'Taslak', color: 'bg-gray-100 text-gray-800 dark:bg-gray-800/50 dark:text-gray-300' },
            'Aktif': { icon: '✅', text: 'Aktif', color: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
            'Tamamlandı': { icon: '🏁', text: 'Tamamlandı', color: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300' },
            'İptal': { icon: '🚫', text: 'İptal', color: 'bg-gray-200 text-gray-700 dark:bg-gray-700/50 dark:text-gray-400' }
        };
        const c = config[status] || config['Taslak'];
        return <span className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
    };

    const handleStatusChange = async (newStatus, endpoint, actionTitle, successMessage) => {
        const result = await Swal.fire({
            title: actionTitle,
            html: `<p><strong>${campaign.name}</strong> kampanyasını <strong>${newStatus}</strong> olarak işaretlemek istediğinize emin misiniz?</p>`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#4f46e5',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Evet',
            cancelButtonText: 'İptal'
        });

        if (result.isConfirmed) {
            setUpdating(true);
            try {
                await api.post(`/campaigns/${id}/${endpoint}`);
                toast.success(successMessage);
                fetchCampaign();
            } catch (error) {
                toast.error(error.response?.data?.message || 'İşlem başarısız');
            } finally {
                setUpdating(false);
            }
        }
    };

    const handleDelete = async () => {
        const result = await Swal.fire({
            title: 'Kampanyayı Sil',
            html: `<p><strong>${campaign.name}</strong> kampanyasını silmek istediğinize emin misiniz?</p>
                   <p class="text-red-500 text-sm">Bu işlem geri alınamaz!</p>`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Evet, Sil',
            cancelButtonText: 'İptal'
        });

        if (result.isConfirmed) {
            try {
                await api.delete(`/campaigns/${id}`);
                toast.success('Kampanya silindi');
                navigate('/campaigns');
            } catch (error) {
                toast.error(error.response?.data?.message || 'Silme başarısız');
            }
        }
    };

    if (loading) {
        return (
            <div className="flex justify-center items-center h-96">
                <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
            </div>
        );
    }

    if (!campaign) return null;

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-blue-50 py-8">
            <div className="max-w-6xl mx-auto px-6">
                {/* Header */}
                <div className="relative bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-2xl overflow-hidden mb-8">
                    <div className="relative px-6 py-8">
                        <div className="flex flex-wrap justify-between items-start gap-4">
                            <div>
                                <Link to="/campaigns" className="inline-flex items-center gap-2 text-white/80 hover:text-white mb-4">
                                    ← Tüm Kampanyalar
                                </Link>
                                <h1 className="text-3xl font-bold">{campaign.name}</h1>
                                <div className="flex flex-wrap gap-3 mt-2">
                                    {getStatusBadge(campaign.status)}
                                    <span className="text-white/80 text-sm">
                                        Oluşturan: {campaign.createdByPersonelName || 'Sistem'} • {formatDate(campaign.createdAt)}
                                    </span>
                                </div>
                            </div>
                            <div className="flex gap-3 flex-wrap">
                                {canEdit && campaign.status !== 'Tamamlandı' && campaign.status !== 'İptal' && (
                                    <Link to={`/campaigns/edit/${campaign.id}`} className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 backdrop-blur-sm rounded-xl hover:bg-white/30">
                                        ✏️ Düzenle
                                    </Link>
                                )}
                                {canEdit && campaign.status === 'Taslak' && (
                                    <button 
                                        onClick={() => handleStatusChange('Aktif', 'activate', 'Kampanyayı Aktifleştir', 'Kampanya aktifleştirildi')}
                                        disabled={updating}
                                        className="inline-flex items-center gap-2 px-4 py-2 bg-green-500/80 backdrop-blur-sm rounded-xl hover:bg-green-600"
                                    >
                                        ✅ Aktifleştir
                                    </button>
                                )}
                                {canEdit && campaign.status === 'Aktif' && (
                                    <button 
                                        onClick={() => handleStatusChange('Tamamlandı', 'complete', 'Kampanyayı Tamamla', 'Kampanya tamamlandı')}
                                        disabled={updating}
                                        className="inline-flex items-center gap-2 px-4 py-2 bg-blue-500/80 backdrop-blur-sm rounded-xl hover:bg-blue-600"
                                    >
                                        🏁 Tamamla
                                    </button>
                                )}
                                {canEdit && campaign.status !== 'Tamamlandı' && campaign.status !== 'İptal' && (
                                    <button 
                                        onClick={() => handleStatusChange('İptal', 'cancel', 'Kampanyayı İptal Et', 'Kampanya iptal edildi')}
                                        disabled={updating}
                                        className="inline-flex items-center gap-2 px-4 py-2 bg-gray-500/80 backdrop-blur-sm rounded-xl hover:bg-gray-600"
                                    >
                                        🚫 İptal Et
                                    </button>
                                )}
                                {canEdit && campaign.status !== 'Tamamlandı' && campaign.status !== 'İptal' && (
                                    <button 
                                        onClick={handleDelete}
                                        disabled={updating}
                                        className="inline-flex items-center gap-2 px-4 py-2 bg-red-600/80 backdrop-blur-sm rounded-xl hover:bg-red-700"
                                    >
                                        🗑️ Sil
                                    </button>
                                )}
                                <button onClick={() => window.print()} className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 backdrop-blur-sm rounded-xl hover:bg-white/30">
                                    🖨️ Yazdır
                                </button>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Detaylar */}
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Sol - Kampanya Bilgileri */}
                    <div className="lg:col-span-2 space-y-6">
                        <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                            <div className="px-6 py-4 bg-gradient-to-r from-amber-50 to-orange-50 border-b border-gray-100">
                                <h2 className="text-lg font-semibold flex items-center gap-2">
                                    <span className="text-xl">📣</span> Kampanya Bilgileri
                                </h2>
                            </div>
                            <div className="p-6 space-y-4">
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500">Kampanya Adı</p>
                                        <p className="font-medium">{campaign.name}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500">Tip</p>
                                        <p className="font-medium">{campaign.type || '-'}</p>
                                    </div>
                                </div>
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500">Başlangıç Tarihi</p>
                                        <p className="font-medium">{formatDate(campaign.startDate)}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500">Bitiş Tarihi</p>
                                        <p className="font-medium">{formatDate(campaign.endDate)}</p>
                                    </div>
                                </div>
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500">Bütçe</p>
                                        <p className="font-bold text-lg text-indigo-600">{formatCurrency(campaign.budget)}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500">Gerçekleşen Maliyet</p>
                                        <p className="font-medium">{campaign.actualCost ? formatCurrency(campaign.actualCost) : '-'}</p>
                                    </div>
                                </div>
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500">Hedef Lead</p>
                                        <p className="font-medium">{campaign.targetLeads || '-'}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500">Dönüşen Lead</p>
                                        <p className="font-medium">{campaign.convertedLeads || '-'}</p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Açıklama */}
                        {campaign.description && (
                            <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-gray-50 to-slate-50 border-b border-gray-100">
                                    <h2 className="text-lg font-semibold flex items-center gap-2">
                                        <span className="text-xl">📝</span> Açıklama
                                    </h2>
                                </div>
                                <div className="p-6">
                                    <p className="text-gray-700 whitespace-pre-wrap">{campaign.description}</p>
                                </div>
                            </div>
                        )}

                        {/* Notlar */}
                        {campaign.notes && (
                            <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-gray-50 to-slate-50 border-b border-gray-100">
                                    <h2 className="text-lg font-semibold flex items-center gap-2">
                                        <span className="text-xl">📝</span> Notlar
                                    </h2>
                                </div>
                                <div className="p-6">
                                    <p className="text-gray-700 whitespace-pre-wrap">{campaign.notes}</p>
                                </div>
                            </div>
                        )}
                    </div>

                    {/* Sağ - Özet */}
                    <div className="lg:col-span-1 space-y-6">
                        <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden sticky top-6">
                            <div className="px-6 py-4 bg-gradient-to-r from-green-50 to-emerald-50 border-b border-gray-100">
                                <h2 className="text-lg font-semibold flex items-center gap-2">
                                    <span className="text-xl">📊</span> Özet
                                </h2>
                            </div>
                            <div className="p-6 space-y-4">
                                <div className="flex justify-between items-center pb-2 border-b">
                                    <span className="text-gray-600">Durum:</span>
                                    {getStatusBadge(campaign.status)}
                                </div>
                                <div className="flex justify-between items-center pb-2 border-b">
                                    <span className="text-gray-600">Bütçe:</span>
                                    <span className="font-bold text-indigo-600">{formatCurrency(campaign.budget)}</span>
                                </div>
                                <div className="flex justify-between items-center pb-2 border-b">
                                    <span className="text-gray-600">Gerçekleşen:</span>
                                    <span className="font-medium">{campaign.actualCost ? formatCurrency(campaign.actualCost) : '-'}</span>
                                </div>
                                <div className="flex justify-between items-center pb-2 border-b">
                                    <span className="text-gray-600">Hedef Lead:</span>
                                    <span className="font-medium">{campaign.targetLeads || '-'}</span>
                                </div>
                                <div className="flex justify-between items-center">
                                    <span className="text-gray-600">Dönüşen Lead:</span>
                                    <span className="font-medium">{campaign.convertedLeads || '-'}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}