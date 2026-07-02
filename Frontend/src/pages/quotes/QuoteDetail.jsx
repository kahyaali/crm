// src/pages/quotes/QuoteDetail.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function QuoteDetail() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [quote, setQuote] = useState(null);
    const [loading, setLoading] = useState(true);
    const [updating, setUpdating] = useState(false);

    const isCreator = quote?.createdByPersonelId === user?.personelId;
    const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
    const canEdit = isAdmin || isCreator;

    useEffect(() => {
        fetchQuote();
    }, [id]);

    const fetchQuote = async () => {
        try {
            setLoading(true);
            const response = await api.get(`/quotes/${id}`);
            setQuote(response.data);
        } catch (error) {
            toast.error('Teklif detayı yüklenemedi');
            navigate('/quotes');
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
            'Gönderildi': { icon: '📨', text: 'Gönderildi', color: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300' },
            'Onaylandı': { icon: '✅', text: 'Onaylandı', color: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
            'Reddedildi': { icon: '❌', text: 'Reddedildi', color: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300' },
            'İptal': { icon: '🚫', text: 'İptal', color: 'bg-gray-200 text-gray-700 dark:bg-gray-700/50 dark:text-gray-400' }
        };
        const c = config[status] || config['Taslak'];
        return <span className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
    };

    const handleStatusChange = async (newStatus, actionTitle, successMessage) => {
        const result = await Swal.fire({
            title: actionTitle,
            html: `<p><strong>${quote.quoteNumber}</strong> nolu teklifi <strong>${newStatus}</strong> olarak işaretlemek istediğinize emin misiniz?</p>`,
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
                const endpoint = newStatus === 'Onaylandı' ? 'approve' : 
                                 newStatus === 'Reddedildi' ? 'reject' : 'cancel';
                await api.post(`/quotes/${id}/${endpoint}`);
                toast.success(successMessage);
                fetchQuote();
            } catch (error) {
                toast.error(error.response?.data?.message || 'İşlem başarısız');
            } finally {
                setUpdating(false);
            }
        }
    };

    const handleDelete = async () => {
        const result = await Swal.fire({
            title: 'Teklifi Sil',
            html: `<p><strong>${quote.quoteNumber}</strong> nolu teklifi silmek istediğinize emin misiniz?</p>
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
                await api.delete(`/quotes/${id}`);
                toast.success('Teklif silindi');
                navigate('/quotes');
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

    if (!quote) return null;

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-blue-50 py-8">
            <div className="max-w-6xl mx-auto px-6">
                {/* Header */}
                <div className="relative bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-2xl overflow-hidden mb-8">
                    <div className="relative px-6 py-8">
                        <div className="flex flex-wrap justify-between items-start gap-4">
                            <div>
                                <Link to="/quotes" className="inline-flex items-center gap-2 text-white/80 hover:text-white mb-4">
                                    ← Tüm Teklifler
                                </Link>
                                <h1 className="text-3xl font-bold">{quote.quoteNumber}</h1>
                                <div className="flex flex-wrap gap-3 mt-2">
                                    {getStatusBadge(quote.status)}
                                    <span className="text-white/80 text-sm">
                                        Oluşturan: {quote.createdByPersonelName || 'Sistem'} • {formatDate(quote.createdAt)}
                                    </span>
                                </div>
                            </div>
                            <div className="flex gap-3 flex-wrap">
                                {canEdit && quote.status !== 'Onaylandı' && quote.status !== 'İptal' && (
                                    <Link to={`/quotes/edit/${quote.id}`} className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 backdrop-blur-sm rounded-xl hover:bg-white/30">
                                        ✏️ Düzenle
                                    </Link>
                                )}
                                {canEdit && quote.status === 'Taslak' && (
                                    <button 
                                        onClick={() => handleStatusChange('Gönderildi', 'Teklifi Gönder', 'Teklif başarıyla gönderildi')}
                                        disabled={updating}
                                        className="inline-flex items-center gap-2 px-4 py-2 bg-blue-500/80 backdrop-blur-sm rounded-xl hover:bg-blue-600"
                                    >
                                        📨 Gönder
                                    </button>
                                )}
                                {canEdit && quote.status === 'Gönderildi' && (
                                    <>
                                        <button 
                                            onClick={() => handleStatusChange('Onaylandı', 'Teklifi Onayla', 'Teklif başarıyla onaylandı')}
                                            disabled={updating}
                                            className="inline-flex items-center gap-2 px-4 py-2 bg-green-500/80 backdrop-blur-sm rounded-xl hover:bg-green-600"
                                        >
                                            ✅ Onayla
                                        </button>
                                        <button 
                                            onClick={() => handleStatusChange('Reddedildi', 'Teklifi Reddet', 'Teklif reddedildi')}
                                            disabled={updating}
                                            className="inline-flex items-center gap-2 px-4 py-2 bg-red-500/80 backdrop-blur-sm rounded-xl hover:bg-red-600"
                                        >
                                            ❌ Reddet
                                        </button>
                                    </>
                                )}
                                {canEdit && quote.status !== 'İptal' && quote.status !== 'Onaylandı' && (
                                    <button 
                                        onClick={() => handleStatusChange('İptal', 'Teklifi İptal Et', 'Teklif iptal edildi')}
                                        disabled={updating}
                                        className="inline-flex items-center gap-2 px-4 py-2 bg-gray-500/80 backdrop-blur-sm rounded-xl hover:bg-gray-600"
                                    >
                                        🚫 İptal Et
                                    </button>
                                )}
                                {canEdit && (
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
                    {/* Sol - Teklif Bilgileri */}
                    <div className="lg:col-span-2 space-y-6">
                        {/* Müşteri Bilgileri */}
                        <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                            <div className="px-6 py-4 bg-gradient-to-r from-amber-50 to-orange-50 border-b border-gray-100">
                                <h2 className="text-lg font-semibold flex items-center gap-2">
                                    <span className="text-xl">🏢</span> Müşteri Bilgileri
                                </h2>
                            </div>
                            <div className="p-6">
                                <p className="font-semibold text-gray-900">{quote.customerName}</p>
                            </div>
                        </div>

                        {/* Fırsat Bilgileri */}
                        {quote.opportunityName && (
                            <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-purple-50 to-pink-50 border-b border-gray-100">
                                    <h2 className="text-lg font-semibold flex items-center gap-2">
                                        <span className="text-xl">🎯</span> Fırsat
                                    </h2>
                                </div>
                                <div className="p-6">
                                    <p className="font-semibold text-gray-900">{quote.opportunityName}</p>
                                </div>
                            </div>
                        )}

                        {/* Tarih Bilgileri */}
                        <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                            <div className="px-6 py-4 bg-gradient-to-r from-blue-50 to-indigo-50 border-b border-gray-100">
                                <h2 className="text-lg font-semibold flex items-center gap-2">
                                    <span className="text-xl">📅</span> Tarih Bilgileri
                                </h2>
                            </div>
                            <div className="p-6">
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500">Teklif Tarihi</p>
                                        <p className="font-medium">{formatDate(quote.quoteDate)}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500">Geçerlilik Tarihi</p>
                                        <p className="font-medium text-orange-600">{formatDate(quote.validUntil)}</p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Teklif Kalemleri */}
                        <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                            <div className="px-6 py-4 bg-gradient-to-r from-gray-50 to-slate-50 border-b border-gray-100">
                                <h2 className="text-lg font-semibold flex items-center gap-2">
                                    <span className="text-xl">📦</span> Teklif Kalemleri
                                </h2>
                            </div>
                            <div className="overflow-x-auto">
                                <table className="w-full">
                                    <thead className="bg-gray-50">
                                        <tr>
                                            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500">Ürün</th>
                                            <th className="px-6 py-3 text-center text-xs font-semibold text-gray-500">Adet</th>
                                            <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500">Birim Fiyat</th>
                                            <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500">Toplam</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-gray-100">
                                        {quote.items?.map((item, idx) => (
                                            <tr key={idx} className="hover:bg-gray-50">
                                                <td className="px-6 py-4">
                                                    <div className="font-medium text-gray-900">{item.productName}</div>
                                                    <div className="text-xs text-gray-400">{item.productCode}</div>
                                                    {item.description && <div className="text-xs text-gray-500">{item.description}</div>}
                                                </td>
                                                <td className="px-6 py-4 text-center">{item.quantity}</td>
                                                <td className="px-6 py-4 text-right">{formatCurrency(item.unitPrice)}</td>
                                                <td className="px-6 py-4 text-right font-medium">{formatCurrency(item.totalPrice)}</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                    <tfoot className="border-t border-gray-200 bg-gray-50">
                                        <tr>
                                            <td colSpan={3} className="px-6 py-3 text-right font-medium">Ara Toplam:</td>
                                            <td className="px-6 py-3 text-right">{formatCurrency(quote.subTotal)}</td>
                                        </tr>
                                        <tr>
                                            <td colSpan={3} className="px-6 py-3 text-right font-medium">KDV (%{quote.taxRate}):</td>
                                            <td className="px-6 py-3 text-right">{formatCurrency(quote.taxAmount)}</td>
                                        </tr>
                                        <tr className="border-t">
                                            <td colSpan={3} className="px-6 py-3 text-right font-bold text-lg">Genel Toplam:</td>
                                            <td className="px-6 py-3 text-right font-bold text-lg text-indigo-600">{formatCurrency(quote.totalAmount)}</td>
                                        </tr>
                                    </tfoot>
                                </table>
                            </div>
                        </div>

                        {/* Notlar */}
                        {quote.notes && (
                            <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-gray-50 to-slate-50 border-b border-gray-100">
                                    <h2 className="text-lg font-semibold flex items-center gap-2">
                                        <span className="text-xl">📝</span> Notlar
                                    </h2>
                                </div>
                                <div className="p-6">
                                    <p className="text-gray-700 whitespace-pre-wrap">{quote.notes}</p>
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
                                    <span className="text-gray-600">Toplam Tutar:</span>
                                    <span className="font-bold text-lg">{formatCurrency(quote.totalAmount)}</span>
                                </div>
                                <div className="flex justify-between items-center">
                                    <span className="text-gray-600">Durum:</span>
                                    {getStatusBadge(quote.status)}
                                </div>
                                <div className="flex justify-between items-center pt-3 border-t">
                                    <span className="text-gray-600">Oluşturan:</span>
                                    <span className="font-medium text-sm">{quote.createdByPersonelName || 'Sistem'}</span>
                                </div>
                                <div className="flex justify-between items-center">
                                    <span className="text-gray-600">Tarih:</span>
                                    <span className="font-medium text-sm">{formatDate(quote.createdAt)}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}