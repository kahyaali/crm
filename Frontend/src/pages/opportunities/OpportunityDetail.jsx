// src/pages/opportunities/OpportunityDetail.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function OpportunityDetail() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [opportunity, setOpportunity] = useState(null);
    const [loading, setLoading] = useState(true);

    const isCreator = opportunity?.createdByPersonelId === user?.personelId;
    const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
    const canEdit = isAdmin || isCreator;

    useEffect(() => {
        fetchOpportunity();
    }, [id]);

    const fetchOpportunity = async () => {
        try {
            setLoading(true);
            const response = await api.get(`/opportunities/${id}`);
            setOpportunity(response.data);
        } catch (error) {
            toast.error('Fırsat detayı yüklenemedi');
            navigate('/opportunities');
        } finally {
            setLoading(false);
        }
    };

    const formatDate = (date) => {
        if (!date) return '-';
        return new Date(date).toLocaleString('tr-TR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    };

    const formatCurrency = (amount) => {
        return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(amount || 0);
    };

    const getStageBadge = (stage) => {
        const config = {
            'Prospekt': { icon: '🎯', text: 'Prospekt', color: 'bg-blue-100 text-blue-700 dark:bg-blue-900/50 dark:text-blue-300' },
            'Teklif': { icon: '📄', text: 'Teklif', color: 'bg-purple-100 text-purple-700 dark:bg-purple-900/50 dark:text-purple-300' },
            'Pazarlık': { icon: '🤝', text: 'Pazarlık', color: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/50 dark:text-yellow-300' },
            'Kapandı-Kazandı': { icon: '✅', text: 'Kazanıldı', color: 'bg-green-100 text-green-700 dark:bg-green-900/50 dark:text-green-300' },
            'Kapandı-Kaybetti': { icon: '❌', text: 'Kaybedildi', color: 'bg-red-100 text-red-700 dark:bg-red-900/50 dark:text-red-300' }
        };
        const c = config[stage] || config['Prospekt'];
        return (
            <span className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium ${c.color}`}>
                {c.icon} {c.text}
            </span>
        );
    };

    const handleMarkAsWon = async () => {
        const result = await Swal.fire({
            title: 'Fırsatı Kazanıldı Olarak İşaretle',
            text: 'Bu fırsatı kazanıldı olarak işaretlemek istediğinize emin misiniz?',
            icon: 'success',
            showCancelButton: true,
            confirmButtonColor: '#22c55e',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Evet, Kazanıldı',
            cancelButtonText: 'İptal'
        });

        if (result.isConfirmed) {
            try {
                await api.post(`/opportunities/${id}/won`);
                toast.success('✅ Fırsat kazanıldı olarak işaretlendi');
                fetchOpportunity();
            } catch (error) {
                toast.error(error.response?.data?.message || 'İşlem başarısız');
            }
        }
    };

    const handleMarkAsLost = async () => {
        const { value: lostReason } = await Swal.fire({
            title: 'Fırsatı Kaybedildi Olarak İşaretle',
            text: 'Kaybetme sebebini girin:',
            input: 'text',
            inputPlaceholder: 'Kaybetme sebebi...',
            showCancelButton: true,
            confirmButtonColor: '#ef4444',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Evet, Kaybedildi',
            cancelButtonText: 'İptal'
        });

        if (lostReason) {
            try {
                await api.post(`/opportunities/${id}/lost`, lostReason);
                toast.success('✅ Fırsat kaybedildi olarak işaretlendi');
                fetchOpportunity();
            } catch (error) {
                toast.error(error.response?.data?.message || 'İşlem başarısız');
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

    if (!opportunity) return null;

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-blue-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 py-8">
            <div className="max-w-6xl mx-auto px-6">
                {/* Header */}
                <div className="relative bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-2xl overflow-hidden mb-8">
                    <div className="relative px-6 py-8">
                        <div className="flex flex-wrap justify-between items-start gap-4">
                            <div>
                                <Link to="/opportunities" className="inline-flex items-center gap-2 text-white/80 hover:text-white mb-4">
                                    ← Tüm Fırsatlar
                                </Link>
                                <h1 className="text-3xl font-bold">{opportunity.name}</h1>
                                <div className="flex flex-wrap gap-3 mt-2">
                                    {getStageBadge(opportunity.stage)}
                                    <span className="text-white/80 text-sm">
                                        Oluşturan: {opportunity.createdByPersonelName || 'Sistem'} • {formatDate(opportunity.createdAt)}
                                    </span>
                                </div>
                            </div>
                            <div className="flex gap-3 flex-wrap">
                                {canEdit && (
                                    <Link to={`/opportunities/edit/${opportunity.id}`} className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 backdrop-blur-sm rounded-xl hover:bg-white/30">
                                        ✏️ Düzenle
                                    </Link>
                                )}
                                {canEdit && opportunity.stage !== 'Kapandı-Kazandı' && opportunity.stage !== 'Kapandı-Kaybetti' && (
                                    <>
                                        <button onClick={handleMarkAsWon} className="inline-flex items-center gap-2 px-4 py-2 bg-green-500/80 backdrop-blur-sm rounded-xl hover:bg-green-600">
                                            ✅ Kazanıldı
                                        </button>
                                        <button onClick={handleMarkAsLost} className="inline-flex items-center gap-2 px-4 py-2 bg-red-500/80 backdrop-blur-sm rounded-xl hover:bg-red-600">
                                            ❌ Kaybedildi
                                        </button>
                                    </>
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
                    <div className="lg:col-span-2 space-y-6">
                        <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg border border-gray-100 dark:border-gray-700 overflow-hidden">
                            <div className="px-6 py-4 bg-gradient-to-r from-amber-50 to-orange-50 dark:from-amber-900/20 dark:to-orange-900/20 border-b border-gray-100 dark:border-gray-700">
                                <h2 className="text-lg font-semibold flex items-center gap-2 text-gray-900 dark:text-white">
                                    <span className="text-xl">📋</span> Fırsat Bilgileri
                                </h2>
                            </div>
                            <div className="p-6 space-y-4">
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">Fırsat Adı</p>
                                        <p className="font-medium text-gray-900 dark:text-white">{opportunity.name}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">Müşteri</p>
                                        <p className="font-medium text-gray-900 dark:text-white">{opportunity.customerName}</p>
                                    </div>
                                </div>
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">Tutar</p>
                                        <p className="font-bold text-lg text-indigo-600 dark:text-indigo-400">{formatCurrency(opportunity.amount)}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">Aşama</p>
                                        {getStageBadge(opportunity.stage)}
                                    </div>
                                </div>
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">Atanan Personel</p>
                                        <p className="font-medium text-gray-900 dark:text-white">{opportunity.assignedToPersonelName || '-'}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">Beklenen Kapanış</p>
                                        <p className="font-medium text-gray-900 dark:text-white">{formatDate(opportunity.expectedCloseDate)}</p>
                                    </div>
                                </div>
                                {opportunity.actualCloseDate && (
                                    <div>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">Gerçek Kapanış Tarihi</p>
                                        <p className="font-medium text-gray-900 dark:text-white">{formatDate(opportunity.actualCloseDate)}</p>
                                    </div>
                                )}
                                {opportunity.lostReason && (
                                    <div>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">Kaybetme Sebebi</p>
                                        <p className="font-medium text-red-600 dark:text-red-400">{opportunity.lostReason}</p>
                                    </div>
                                )}
                            </div>
                        </div>

                        {opportunity.description && (
                            <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg border border-gray-100 dark:border-gray-700 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-gray-50 to-slate-50 dark:from-gray-700/50 dark:to-slate-700/50 border-b border-gray-100 dark:border-gray-700">
                                    <h2 className="text-lg font-semibold flex items-center gap-2 text-gray-900 dark:text-white">
                                        <span className="text-xl">📝</span> Açıklama
                                    </h2>
                                </div>
                                <div className="p-6">
                                    <p className="text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{opportunity.description}</p>
                                </div>
                            </div>
                        )}
                    </div>

                    <div className="lg:col-span-1 space-y-6">
                        <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg border border-gray-100 dark:border-gray-700 overflow-hidden sticky top-6">
                            <div className="px-6 py-4 bg-gradient-to-r from-green-50 to-emerald-50 dark:from-green-900/20 dark:to-emerald-900/20 border-b border-gray-100 dark:border-gray-700">
                                <h2 className="text-lg font-semibold flex items-center gap-2 text-gray-900 dark:text-white">
                                    <span className="text-xl">📊</span> Özet
                                </h2>
                            </div>
                            <div className="p-6 space-y-4">
                                <div className="flex justify-between items-center pb-2 border-b border-gray-100 dark:border-gray-700">
                                    <span className="text-gray-600 dark:text-gray-400">Aşama:</span>
                                    {getStageBadge(opportunity.stage)}
                                </div>
                                <div className="flex justify-between items-center pb-2 border-b border-gray-100 dark:border-gray-700">
                                    <span className="text-gray-600 dark:text-gray-400">Tutar:</span>
                                    <span className="font-bold text-indigo-600 dark:text-indigo-400">{formatCurrency(opportunity.amount)}</span>
                                </div>
                                <div className="flex justify-between items-center pb-2 border-b border-gray-100 dark:border-gray-700">
                                    <span className="text-gray-600 dark:text-gray-400">Atanan:</span>
                                    <span className="font-medium text-gray-900 dark:text-white">{opportunity.assignedToPersonelName || '-'}</span>
                                </div>
                                <div className="flex justify-between items-center">
                                    <span className="text-gray-600 dark:text-gray-400">Durum:</span>
                                    <span className={`font-medium text-sm ${opportunity.stage === 'Kapandı-Kazandı' ? 'text-green-600 dark:text-green-400' : opportunity.stage === 'Kapandı-Kaybetti' ? 'text-red-600 dark:text-red-400' : 'text-blue-600 dark:text-blue-400'}`}>
                                        {opportunity.stage === 'Kapandı-Kazandı' ? '✅ Kazanıldı' :
                                         opportunity.stage === 'Kapandı-Kaybetti' ? '❌ Kaybedildi' :
                                         '🔄 Devam Ediyor'}
                                    </span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}