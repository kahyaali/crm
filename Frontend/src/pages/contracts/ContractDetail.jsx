// src/pages/contracts/ContractDetail.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function ContractDetail() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [contract, setContract] = useState(null);
    const [loading, setLoading] = useState(true);
    const [updating, setUpdating] = useState(false);

    const isCreator = contract?.createdByPersonelId === user?.personelId;
    const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
    const canEdit = isAdmin || isCreator;

    useEffect(() => {
        fetchContract();
    }, [id]);

    const fetchContract = async () => {
        try {
            setLoading(true);
            const response = await api.get(`/contracts/${id}`);
            setContract(response.data);
        } catch (error) {
            toast.error('Sözleşme detayı yüklenemedi');
            navigate('/contracts');
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
            'Bekliyor': { icon: '⏳', text: 'Bekliyor', color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300' },
            'Aktif': { icon: '✅', text: 'Aktif', color: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
            'Süresi Doldu': { icon: '⏰', text: 'Süresi Doldu', color: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300' },
            'Feshedildi': { icon: '🚫', text: 'Feshedildi', color: 'bg-gray-200 text-gray-700 dark:bg-gray-700/50 dark:text-gray-400' }
        };
        const c = config[status] || config['Taslak'];
        return <span className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
    };

    const handleSign = async () => {
        const result = await Swal.fire({
            title: 'Sözleşmeyi İmzala',
            html: `<p><strong>${contract.contractNumber}</strong> nolu sözleşmeyi imzalamak istediğinize emin misiniz?</p>`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#4f46e5',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Evet, İmzala',
            cancelButtonText: 'İptal'
        });

        if (result.isConfirmed) {
            setUpdating(true);
            try {
                await api.post(`/contracts/${id}/sign`, { signedBy: user?.fullName || user?.email });
                toast.success('Sözleşme imzalandı');
                fetchContract();
            } catch (error) {
                toast.error(error.response?.data?.message || 'İmzalama başarısız');
            } finally {
                setUpdating(false);
            }
        }
    };

    const handleTerminate = async () => {
        const result = await Swal.fire({
            title: 'Sözleşmeyi Feshet',
            html: `<p><strong>${contract.contractNumber}</strong> nolu sözleşmeyi feshetmek istediğinize emin misiniz?</p>
                   <p class="text-red-500 text-sm">Bu işlem geri alınamaz!</p>`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Evet, Feshet',
            cancelButtonText: 'İptal'
        });

        if (result.isConfirmed) {
            setUpdating(true);
            try {
                await api.post(`/contracts/${id}/terminate`);
                toast.success('Sözleşme feshedildi');
                fetchContract();
            } catch (error) {
                toast.error(error.response?.data?.message || 'Feshetme başarısız');
            } finally {
                setUpdating(false);
            }
        }
    };

    const handleDelete = async () => {
        const result = await Swal.fire({
            title: 'Sözleşmeyi Sil',
            html: `<p><strong>${contract.contractNumber}</strong> nolu sözleşmeyi silmek istediğinize emin misiniz?</p>
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
                await api.delete(`/contracts/${id}`);
                toast.success('Sözleşme silindi');
                navigate('/contracts');
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

    if (!contract) return null;

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-blue-50 py-8">
            <div className="max-w-6xl mx-auto px-6">
                {/* Header */}
                <div className="relative bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-2xl overflow-hidden mb-8">
                    <div className="relative px-6 py-8">
                        <div className="flex flex-wrap justify-between items-start gap-4">
                            <div>
                                <Link to="/contracts" className="inline-flex items-center gap-2 text-white/80 hover:text-white mb-4">
                                    ← Tüm Sözleşmeler
                                </Link>
                                <h1 className="text-3xl font-bold">{contract.contractNumber}</h1>
                                <div className="flex flex-wrap gap-3 mt-2">
                                    {getStatusBadge(contract.status)}
                                    <span className="text-white/80 text-sm">
                                        Oluşturan: {contract.createdByPersonelName || 'Sistem'} • {formatDate(contract.createdAt)}
                                    </span>
                                </div>
                            </div>
                            <div className="flex gap-3 flex-wrap">
                                {canEdit && contract.status === 'Taslak' && (
                                    <Link to={`/contracts/edit/${contract.id}`} className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 backdrop-blur-sm rounded-xl hover:bg-white/30">
                                        ✏️ Düzenle
                                    </Link>
                                )}
                                {canEdit && contract.status === 'Bekliyor' && (
                                    <button 
                                        onClick={handleSign}
                                        disabled={updating}
                                        className="inline-flex items-center gap-2 px-4 py-2 bg-green-500/80 backdrop-blur-sm rounded-xl hover:bg-green-600"
                                    >
                                        ✍️ İmzala
                                    </button>
                                )}
                                {(contract.status === 'Aktif' || contract.status === 'Bekliyor') && (
                                    <button 
                                        onClick={handleTerminate}
                                        disabled={updating}
                                        className="inline-flex items-center gap-2 px-4 py-2 bg-red-500/80 backdrop-blur-sm rounded-xl hover:bg-red-600"
                                    >
                                        🚫 Feshet
                                    </button>
                                )}
                                {canEdit && contract.status !== 'Aktif' && contract.status !== 'Süresi Doldu' && contract.status !== 'Feshedildi' && (
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
                    {/* Sol - Sözleşme Bilgileri */}
                    <div className="lg:col-span-2 space-y-6">
                        <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                            <div className="px-6 py-4 bg-gradient-to-r from-amber-50 to-orange-50 border-b border-gray-100">
                                <h2 className="text-lg font-semibold flex items-center gap-2">
                                    <span className="text-xl">📄</span> Sözleşme Bilgileri
                                </h2>
                            </div>
                            <div className="p-6 space-y-4">
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500">Başlık</p>
                                        <p className="font-medium">{contract.title}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500">Müşteri</p>
                                        <p className="font-medium">{contract.customerName}</p>
                                    </div>
                                </div>
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500">Başlangıç Tarihi</p>
                                        <p className="font-medium">{formatDate(contract.startDate)}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500">Bitiş Tarihi</p>
                                        <p className="font-medium">{formatDate(contract.endDate)}</p>
                                    </div>
                                </div>
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500">Sözleşme Değeri</p>
                                        <p className="font-bold text-lg text-indigo-600">{formatCurrency(contract.contractValue)}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500">Durum</p>
                                        {getStatusBadge(contract.status)}
                                    </div>
                                </div>
                                {contract.quoteNumber && (
                                    <div>
                                        <p className="text-xs text-gray-500">İlişkili Teklif</p>
                                        <p className="font-medium">{contract.quoteNumber}</p>
                                    </div>
                                )}
                            </div>
                        </div>

                        {/* Açıklama */}
                        {contract.description && (
                            <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-gray-50 to-slate-50 border-b border-gray-100">
                                    <h2 className="text-lg font-semibold flex items-center gap-2">
                                        <span className="text-xl">📝</span> Açıklama
                                    </h2>
                                </div>
                                <div className="p-6">
                                    <p className="text-gray-700 whitespace-pre-wrap">{contract.description}</p>
                                </div>
                            </div>
                        )}

                        {/* Notlar */}
                        {contract.notes && (
                            <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-gray-50 to-slate-50 border-b border-gray-100">
                                    <h2 className="text-lg font-semibold flex items-center gap-2">
                                        <span className="text-xl">📝</span> Notlar
                                    </h2>
                                </div>
                                <div className="p-6">
                                    <p className="text-gray-700 whitespace-pre-wrap">{contract.notes}</p>
                                </div>
                            </div>
                        )}

                        {/* İmza Bilgileri */}
                        {contract.isSigned && (
                            <div className="bg-white rounded-2xl shadow-lg border border-green-200 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-green-50 to-emerald-50 border-b border-green-200">
                                    <h2 className="text-lg font-semibold flex items-center gap-2">
                                        <span className="text-xl">✍️</span> İmza Bilgileri
                                    </h2>
                                </div>
                                <div className="p-6 grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500">İmzalayan</p>
                                        <p className="font-medium">{contract.signedBy}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500">İmza Tarihi</p>
                                        <p className="font-medium">{formatDate(contract.signedDate)}</p>
                                    </div>
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
                                    <span className="text-gray-600">Sözleşme No:</span>
                                    <span className="font-bold text-sm">{contract.contractNumber}</span>
                                </div>
                                <div className="flex justify-between items-center pb-2 border-b">
                                    <span className="text-gray-600">Değer:</span>
                                    <span className="font-bold text-lg text-indigo-600">{formatCurrency(contract.contractValue)}</span>
                                </div>
                                <div className="flex justify-between items-center">
                                    <span className="text-gray-600">Durum:</span>
                                    {getStatusBadge(contract.status)}
                                </div>
                                <div className="flex justify-between items-center pt-3 border-t">
                                    <span className="text-gray-600">İmza:</span>
                                    <span className={`font-medium text-sm ${contract.isSigned ? 'text-green-600' : 'text-red-500'}`}>
                                        {contract.isSigned ? '✅ İmzalandı' : '❌ İmzalanmadı'}
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