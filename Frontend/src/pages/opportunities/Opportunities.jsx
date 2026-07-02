// src/pages/opportunities/Opportunities.jsx
import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useSignalR } from '../../contexts/SignalRContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function Opportunities() {
    const { user } = useAuth();
    const { isConnected, refreshSignal } = useSignalR();
    const [opportunities, setOpportunities] = useState([]);
    const [loading, setLoading] = useState(true);
    
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [totalCount, setTotalCount] = useState(0);
    const pageSize = 10;
    
    const [filters, setFilters] = useState({
        search: '',
        stage: '',
        startDate: '',
        endDate: ''
    });
    
    const [customers, setCustomers] = useState([]);
    const [personels, setPersonels] = useState([]);
    const [stageOptions] = useState([
        { value: 'Prospekt', label: '🎯 Prospekt' },
        { value: 'Teklif', label: '📄 Teklif' },
        { value: 'Pazarlık', label: '🤝 Pazarlık' },
        { value: 'Kapandı-Kazandı', label: '✅ Kazanıldı' },
        { value: 'Kapandı-Kaybetti', label: '❌ Kaybedildi' }
    ]);

    useEffect(() => {
        fetchCustomers();
        fetchPersonels();
    }, []);

    useEffect(() => {
        fetchOpportunities();
    }, [page, filters, refreshSignal]);

    const fetchCustomers = async () => {
        try {
            const response = await api.get('/opportunities/customers');
            setCustomers(response.data || []);
        } catch (error) {
            console.error('Müşteriler yüklenemedi:', error);
        }
    };

    const fetchPersonels = async () => {
        try {
            const response = await api.get('/opportunities/personels');
            setPersonels(response.data || []);
        } catch (error) {
            console.error('Personeller yüklenemedi:', error);
        }
    };

    const fetchOpportunities = useCallback(async () => {
        try {
            setLoading(true);
            const params = { page, pageSize, ...filters };
            const response = await api.get('/opportunities', { params });
            setOpportunities(response.data.data || []);
            setTotalPages(response.data.totalPages || 1);
            setTotalCount(response.data.totalCount || 0);
        } catch (error) {
            toast.error('Fırsatlar yüklenemedi');
        } finally {
            setLoading(false);
        }
    }, [page, filters]);

    const handleFilterChange = (key, value) => {
        setFilters(prev => ({ ...prev, [key]: value }));
        setPage(1);
    };

    const handleClearFilters = () => {
        setFilters({ search: '', stage: '', startDate: '', endDate: '' });
        setPage(1);
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

    const handleDelete = async (opportunity) => {
        const result = await Swal.fire({
            title: 'Fırsatı Sil',
            html: `<div style="text-align: left;">
                        <p><strong>${opportunity.name}</strong> fırsatını silmek üzeresiniz.</p>
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
                await api.delete(`/opportunities/${opportunity.id}`);
                Swal.fire({
                    title: 'Silindi!',
                    text: `${opportunity.name} fırsatı silindi.`,
                    icon: 'success',
                    confirmButtonColor: '#4f46e5',
                    timer: 2000
                });
                fetchOpportunities();
            } catch (error) {
                Swal.fire({
                    title: 'Hata!',
                    text: error.response?.data?.message || 'Fırsat silinemedi.',
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
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Fırsatlar</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400">
                        Toplam {totalCount} fırsat
                        {isConnected && <span className="ml-2 text-green-500 text-xs">🟢 Canlı</span>}
                    </p>
                </div>
                <Link
                    to="/opportunities/create"
                    className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg transition-colors flex items-center gap-2"
                >
                    <span>+</span> Yeni Fırsat
                </Link>
            </div>

            {/* Filtreler */}
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4 mb-6">
                <div className="flex flex-wrap gap-3">
                    <input
                        type="text"
                        placeholder="Fırsat adı, müşteri ara..."
                        value={filters.search}
                        onChange={(e) => handleFilterChange('search', e.target.value)}
                        className="flex-1 min-w-[200px] px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                    />
                    <select
                        value={filters.stage}
                        onChange={(e) => handleFilterChange('stage', e.target.value)}
                        className="px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200"
                    >
                        <option value="">Tüm Aşamalar</option>
                        {stageOptions.map(opt => (
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
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Fırsat</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Müşteri</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tutar</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Aşama</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Atanan</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Beklenen Kapanış</th>
                                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                            {opportunities.length === 0 ? (
                                <tr>
                                    <td colSpan={7} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                                        Henüz fırsat bulunmuyor
                                    </td>
                                </tr>
                            ) : (
                                opportunities.map((opportunity) => {
                                    const isCreator = String(opportunity.createdByPersonelId) === String(user?.personelId);
                                    return (
                                        <tr key={opportunity.id} className="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                                            <td className="px-6 py-4 text-sm font-medium text-gray-900 dark:text-white">
                                                {opportunity.name}
                                            </td>
                                            <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-300">
                                                {opportunity.customerName}
                                            </td>
                                            <td className="px-6 py-4 text-sm text-right font-semibold text-gray-900 dark:text-white">
                                                {formatCurrency(opportunity.amount)}
                                            </td>
                                            <td className="px-6 py-4">
                                                {getStageBadge(opportunity.stage)}
                                            </td>
                                            <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-300">
                                                {opportunity.assignedToPersonelName || '-'}
                                            </td>
                                            <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-300">
                                                {formatDate(opportunity.expectedCloseDate)}
                                            </td>
                                            <td className="px-6 py-4 text-center">
                                                <div className="flex justify-center gap-2">
                                                    <Link
                                                        to={`/opportunities/${opportunity.id}`}
                                                        className="text-emerald-600 dark:text-emerald-400 hover:text-emerald-800 dark:hover:text-emerald-300 transition-colors"
                                                        title="Detay"
                                                    >
                                                        👁️
                                                    </Link>
                                                    {isCreator && (
                                                        <Link
                                                            to={`/opportunities/edit/${opportunity.id}`}
                                                            className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition-colors"
                                                            title="Düzenle"
                                                        >
                                                            ✏️
                                                        </Link>
                                                    )}
                                                    {isCreator && (
                                                        <button
                                                            onClick={() => handleDelete(opportunity)}
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