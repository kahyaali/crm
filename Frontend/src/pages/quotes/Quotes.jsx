// src/pages/quotes/Quotes.jsx
import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useSignalR } from '../../contexts/SignalRContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function Quotes() {
    const { user } = useAuth();
    const { isConnected, refreshSignal } = useSignalR();
    const [quotes, setQuotes] = useState([]);
    const [loading, setLoading] = useState(true);
    
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [totalCount, setTotalCount] = useState(0);
    const pageSize = 10;
    
    const [filters, setFilters] = useState({
        search: '',
        status: '',
        startDate: '',
        endDate: ''
    });
    
    const [customers, setCustomers] = useState([]);
    const [statusOptions] = useState([
        { value: 'Taslak', label: '📝 Taslak' },
        { value: 'Gönderildi', label: '📨 Gönderildi' },
        { value: 'Onaylandı', label: '✅ Onaylandı' },
        { value: 'Reddedildi', label: '❌ Reddedildi' },
        { value: 'İptal', label: '🚫 İptal' }
    ]);

    useEffect(() => {
        fetchCustomers();
    }, []);

    useEffect(() => {
        fetchQuotes();
    }, [page, filters, refreshSignal]);

    const fetchCustomers = async () => {
        try {
            const response = await api.get('/quotes/customers');
            setCustomers(response.data || []);
        } catch (error) {
            console.error('Müşteriler yüklenemedi:', error);
        }
    };

    const fetchQuotes = useCallback(async () => {
        try {
            setLoading(true);
            const params = { page, pageSize, ...filters };
            const response = await api.get('/quotes', { params });
            setQuotes(response.data.data || []);
            setTotalPages(response.data.totalPages || 1);
            setTotalCount(response.data.totalCount || 0);
        } catch (error) {
            toast.error('Teklifler yüklenemedi');
        } finally {
            setLoading(false);
        }
    }, [page, filters]);

    const handleFilterChange = (key, value) => {
        setFilters(prev => ({ ...prev, [key]: value }));
        setPage(1);
    };

    const handleClearFilters = () => {
        setFilters({ search: '', status: '', startDate: '', endDate: '' });
        setPage(1);
    };

    const getStatusBadge = (status) => {
        const config = {
            'Taslak': { icon: '📝', text: 'Taslak', color: 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300' },
            'Gönderildi': { icon: '📨', text: 'Gönderildi', color: 'bg-blue-100 text-blue-700 dark:bg-blue-900/50 dark:text-blue-300' },
            'Onaylandı': { icon: '✅', text: 'Onaylandı', color: 'bg-green-100 text-green-700 dark:bg-green-900/50 dark:text-green-300' },
            'Reddedildi': { icon: '❌', text: 'Reddedildi', color: 'bg-red-100 text-red-700 dark:bg-red-900/50 dark:text-red-300' },
            'İptal': { icon: '🚫', text: 'İptal', color: 'bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-400' }
        };
        const c = config[status] || config['Taslak'];
        return (
            <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium ${c.color}`}>
                {c.icon} {c.text}
            </span>
        );
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

    const handleDelete = async (quote) => {
        const result = await Swal.fire({
            title: 'Teklifi Sil',
            html: `<div style="text-align: left;">
                        <p><strong>${quote.quoteNumber}</strong> nolu teklifi silmek üzeresiniz.</p>
                        <p class="text-warning" style="color: #e74c3c; margin-top: 12px;">Bu işlem geri alınamaz!</p>
                   </div>`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Evet, Sil',
            cancelButtonText: 'İptal',
            reverseButtons: true
        });
        
        if (result.isConfirmed) {
            try {
                await api.delete(`/quotes/${quote.id}`);
                Swal.fire({
                    title: 'Silindi!',
                    text: `${quote.quoteNumber} nolu teklif silindi.`,
                    icon: 'success',
                    confirmButtonColor: '#4f46e5',
                    timer: 2000
                });
                fetchQuotes();
            } catch (error) {
                Swal.fire({
                    title: 'Hata!',
                    text: error.response?.data?.message || 'Teklif silinemedi.',
                    icon: 'error',
                    confirmButtonColor: '#4f46e5'
                });
            }
        }
    };

    if (loading && page === 1) {
        return (
            <div className="flex justify-center items-center h-64">
                <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 py-8">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Teklifler</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400">
                        Toplam {totalCount} teklif
                        {isConnected && <span className="ml-2 text-green-500 text-xs">🟢 Canlı</span>}
                    </p>
                </div>
                <Link
                    to="/quotes/create"
                    className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg transition-colors flex items-center gap-2"
                >
                    <span>+</span> Yeni Teklif
                </Link>
            </div>

            {/* Filtreler */}
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4 mb-6">
                <div className="flex flex-wrap gap-3">
                    <input
                        type="text"
                        placeholder="Teklif No, Müşteri ara..."
                        value={filters.search}
                        onChange={(e) => handleFilterChange('search', e.target.value)}
                        className="flex-1 min-w-[200px] px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                    />
                    <select
                        value={filters.status}
                        onChange={(e) => handleFilterChange('status', e.target.value)}
                        className="px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                    >
                        <option value="">Tüm Durumlar</option>
                        {statusOptions.map(opt => (
                            <option key={opt.value} value={opt.value}>{opt.label}</option>
                        ))}
                    </select>
                    <input
                        type="date"
                        placeholder="Başlangıç"
                        value={filters.startDate}
                        onChange={(e) => handleFilterChange('startDate', e.target.value)}
                        className="px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                    />
                    <input
                        type="date"
                        placeholder="Bitiş"
                        value={filters.endDate}
                        onChange={(e) => handleFilterChange('endDate', e.target.value)}
                        className="px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                    />
                    <button
                        onClick={handleClearFilters}
                        className="px-4 py-2 bg-gray-500 hover:bg-gray-600 text-white rounded-lg transition-colors text-sm font-medium"
                    >
                        Temizle
                    </button>
                </div>
            </div>

            {/* Tablo */}
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
                <div className="overflow-x-auto">
                    <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                        <thead className="bg-gray-50 dark:bg-gray-700/50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Teklif No</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Müşteri</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tarih</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Geçerlilik</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tutar</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                            {quotes.length === 0 ? (
                                <tr>
                                    <td colSpan={7} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                                        Henüz teklif bulunmuyor
                                    </td>
                                </tr>
                            ) : (
                                quotes.map((quote) => {
                                    const isCreator = String(quote.createdByPersonelId) === String(user?.personelId);
                                    return (
                                        <tr key={quote.id} className="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                                            <td className="px-6 py-4 text-sm font-medium text-gray-900 dark:text-white">
                                                {quote.quoteNumber}
                                            </td>
                                            <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-300">
                                                {quote.customerName}
                                            </td>
                                            <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-300">
                                                {formatDate(quote.quoteDate)}
                                            </td>
                                            <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-300">
                                                {formatDate(quote.validUntil)}
                                            </td>
                                            <td className="px-6 py-4 text-sm text-right font-semibold text-gray-900 dark:text-white">
                                                {formatCurrency(quote.totalAmount)}
                                            </td>
                                            <td className="px-6 py-4">
                                                {getStatusBadge(quote.status)}
                                            </td>
                                            <td className="px-6 py-4 text-center">
                                                <div className="flex justify-center gap-2">
                                                    <Link
                                                        to={`/quotes/${quote.id}`}
                                                        className="text-emerald-600 dark:text-emerald-400 hover:text-emerald-800 dark:hover:text-emerald-300 transition-colors"
                                                        title="Detay"
                                                    >
                                                        👁️
                                                    </Link>
                                                    {isCreator && quote.status !== 'Onaylandı' && quote.status !== 'İptal' && (
                                                        <Link
                                                            to={`/quotes/edit/${quote.id}`}
                                                            className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition-colors"
                                                            title="Düzenle"
                                                        >
                                                            ✏️
                                                        </Link>
                                                    )}
                                                    {isCreator && quote.status !== 'Onaylandı' && quote.status !== 'İptal' && (
                                                        <button
                                                            onClick={() => handleDelete(quote)}
                                                            className="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300 transition-colors"
                                                            title="Sil"
                                                        >
                                                            🗑️
                                                        </button>
                                                    )}
                                                </div>
                                            </td>
                                        </tr>
                                    );
                                })
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="flex justify-between items-center mt-4 pt-2">
                    <div className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} kayıt</div>
                    <div className="flex gap-2">
                        <button
                            onClick={() => setPage(p => Math.max(1, p - 1))}
                            disabled={page === 1}
                            className="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded-lg disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-sm"
                        >
                            ◀
                        </button>
                        <span className="px-3 py-1 text-sm text-gray-700 dark:text-gray-300">Sayfa {page} / {totalPages}</span>
                        <button
                            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                            disabled={page === totalPages}
                            className="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded-lg disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-sm"
                        >
                            ▶
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}