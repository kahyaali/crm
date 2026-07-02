// src/pages/meetings/Meetings.jsx
import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useSignalR } from '../../contexts/SignalRContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function Meetings() {
    const { user } = useAuth();
    const { isConnected, refreshSignal } = useSignalR();
    const [meetings, setMeetings] = useState([]);
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
    
    const [statusOptions, setStatusOptions] = useState([]);

    useEffect(() => {
        fetchStatusList();
    }, []);

    useEffect(() => {
        fetchMeetings();
    }, [page, filters, refreshSignal]);

    const fetchStatusList = async () => {
        try {
            const response = await api.get('/meetings/status-list');
            setStatusOptions(response.data);
        } catch (error) {
            console.error('Durum listesi yüklenemedi:', error);
        }
    };

    const fetchMeetings = useCallback(async () => {
        try {
            setLoading(true);
            const params = { page, pageSize, ...filters };
            const response = await api.get('/meetings', { params });
            setMeetings(response.data.data || []);
            setTotalPages(response.data.totalPages || 1);
            setTotalCount(response.data.totalCount || 0);
        } catch (error) {
            toast.error('Toplantılar yüklenemedi');
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
            'Planlandı': { icon: '📅', text: 'Planlandı', color: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300' },
            'Devam Ediyor': { icon: '🔄', text: 'Devam Ediyor', color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300' },
            'Tamamlandı': { icon: '✅', text: 'Tamamlandı', color: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
            'İptal': { icon: '❌', text: 'İptal', color: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300' }
        };
        const c = config[status] || config['Planlandı'];
        return <span className={`px-2 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
    };

    const formatDate = (date) => {
        return new Date(date).toLocaleString('tr-TR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    // 🔥 PROFESYONEL SİLME FONKSİYONU
    const handleDelete = async (meeting) => {
        const result = await Swal.fire({
            title: 'Toplantıyı Sil',
            html: `<div style="text-align: left;">
                        <p><strong>${meeting.title}</strong> toplantısını silmek üzeresiniz.</p>
                        <p class="text-warning" style="color: #e74c3c; margin-top: 12px;">Bu işlem geri alınamaz!</p>
                   </div>`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Evet, Sil',
            cancelButtonText: 'İptal',
            reverseButtons: true,
            background: '#fff',
            customClass: {
                popup: 'rounded-xl',
                title: 'text-xl font-bold',
                confirmButton: 'px-4 py-2 rounded-lg',
                cancelButton: 'px-4 py-2 rounded-lg'
            }
        });
        
        if (result.isConfirmed) {
            try {
                await api.delete(`/meetings/${meeting.id}`);
                
                Swal.fire({
                    title: 'Silindi!',
                    text: `${meeting.title} toplantısı başarıyla silindi.`,
                    icon: 'success',
                    confirmButtonColor: '#4f46e5',
                    confirmButtonText: 'Tamam',
                    timer: 2000,
                    showConfirmButton: true
                });
                
                fetchMeetings();
            } catch (error) {
                Swal.fire({
                    title: 'Hata!',
                    text: error.response?.data?.message || 'Toplantı silinemedi.',
                    icon: 'error',
                    confirmButtonColor: '#4f46e5',
                    confirmButtonText: 'Tamam'
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
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Toplantılar</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400">
                        Toplam {totalCount} toplantı
                        {isConnected && <span className="ml-2 text-green-500 text-xs">🟢 Canlı</span>}
                    </p>
                </div>
                <Link
                    to="/meetings/create"
                    className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg transition-colors flex items-center gap-2"
                >
                    <span>+</span> Yeni Toplantı
                </Link>
            </div>

            {/* Filtreler */}
            <div className="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-800 p-4 mb-6">
                <div className="flex flex-wrap gap-3">
                    <input
                        type="text"
                        placeholder="Toplantı adı, müşteri, lead ara..."
                        value={filters.search}
                        onChange={(e) => handleFilterChange('search', e.target.value)}
                        className="flex-1 min-w-[200px] px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 text-sm"
                    />
                    <select
                        value={filters.status}
                        onChange={(e) => handleFilterChange('status', e.target.value)}
                        className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm"
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
                        className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm"
                    />
                    <input
                        type="date"
                        placeholder="Bitiş"
                        value={filters.endDate}
                        onChange={(e) => handleFilterChange('endDate', e.target.value)}
                        className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm"
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
            <div className="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-800 overflow-hidden">
                <div className="overflow-x-auto">
                    <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-800">
                        <thead className="bg-gray-50 dark:bg-gray-800/50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Başlık</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tarih/Saat</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Konum/Link</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Katılımcılar</th>
                                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
                            {meetings.length === 0 ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                                        Henüz toplantı bulunmuyor
                                    </td>
                                </tr>
                            ) : (
                                meetings.map((meeting) => {
                                    const isCreator = String(meeting.createdByPersonelId) === String(user?.personelId);
                                    return (
                                        <tr key={meeting.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                                            <td className="px-6 py-4 text-gray-900 dark:text-gray-100 font-medium">
                                                {meeting.title}
                                                {meeting.customerName && (
                                                    <div className="text-xs text-gray-500 dark:text-gray-400">
                                                        Müşteri: {meeting.customerName}
                                                    </div>
                                                )}
                                                {meeting.leadName && (
                                                    <div className="text-xs text-gray-500 dark:text-gray-400">
                                                        Lead: {meeting.leadName}
                                                    </div>
                                                )}
                                            </td>
                                            <td className="px-6 py-4 text-gray-600 dark:text-gray-400 text-sm">
                                                {formatDate(meeting.startTime)}
                                                <div className="text-xs">→ {formatDate(meeting.endTime)}</div>
                                            </td>
                                            <td className="px-6 py-4 text-gray-600 dark:text-gray-400 text-sm">
                                                {meeting.location || meeting.meetingLink ? (
                                                    <div>
                                                        {meeting.location && <div>📍 {meeting.location}</div>}
                                                        {meeting.meetingLink && (
                                                            <a href={meeting.meetingLink} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline text-xs">
                                                                🔗 Toplantı Linki
                                                            </a>
                                                        )}
                                                    </div>
                                                ) : '-'}
                                            </td>
                                            <td className="px-6 py-4">{getStatusBadge(meeting.status)}</td>
                                            <td className="px-6 py-4">
                                                <div className="flex -space-x-2">
                                                    {meeting.attendees?.slice(0, 3).map((attendee, idx) => (
                                                        <div key={idx} className="w-8 h-8 rounded-full bg-indigo-100 dark:bg-indigo-900/50 flex items-center justify-center text-xs font-medium text-indigo-700 dark:text-indigo-300 border-2 border-white dark:border-gray-800"
                                                            title={attendee.personelName}>
                                                            {attendee.personelName?.charAt(0) || '?'}
                                                        </div>
                                                    ))}
                                                    {meeting.attendees?.length > 3 && (
                                                        <div className="w-8 h-8 rounded-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center text-xs font-medium text-gray-600 dark:text-gray-400 border-2 border-white dark:border-gray-800">
                                                            +{meeting.attendees.length - 3}
                                                        </div>
                                                    )}
                                                </div>
                                            </td>
                                            <td className="px-6 py-4 text-center">
                                                <div className="flex justify-center gap-2">
                                                    <Link
                                                        to={`/meetings/${meeting.id}`}
                                                        className="text-emerald-600 dark:text-emerald-400 hover:text-emerald-800 dark:hover:text-emerald-300 transition-colors"
                                                        title="Detay"
                                                    >
                                                        👁️
                                                    </Link>
                                                    
                                                    {isCreator && (
                                                        <Link
                                                            to={`/meetings/edit/${meeting.id}`}
                                                            className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition-colors"
                                                            title="Düzenle"
                                                        >
                                                            ✏️
                                                        </Link>
                                                    )}
                                                    
                                                    {isCreator && (
                                                        <button
                                                            onClick={() => handleDelete(meeting)}
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
                        <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="px-3 py-1 border border-gray-200 dark:border-gray-700 rounded-lg disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">◀</button>
                        <span className="px-3 py-1 text-sm text-gray-700 dark:text-gray-300">Sayfa {page} / {totalPages}</span>
                        <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="px-3 py-1 border border-gray-200 dark:border-gray-700 rounded-lg disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">▶</button>
                    </div>
                </div>
            )}
        </div>
    );
}