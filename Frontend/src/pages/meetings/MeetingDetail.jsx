// src/pages/meetings/MeetingDetail.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function MeetingDetail() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [meeting, setMeeting] = useState(null);
    const [loading, setLoading] = useState(true);
    const [updatingUserId, setUpdatingUserId] = useState(null);
    
    // Pagination state'leri
    const [currentPage, setCurrentPage] = useState(1);
    const [itemsPerPage, setItemsPerPage] = useState(10);
    
    const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
    const currentPersonelId = user?.personelId;

    useEffect(() => {
        fetchMeeting();
    }, [id]);

    const fetchMeeting = async () => {
        try {
            setLoading(true);
            const response = await api.get(`/meetings/${id}`);
            setMeeting(response.data);
            setCurrentPage(1);
        } catch (error) {
            toast.error('Toplantı detayı yüklenemedi');
            navigate('/meetings');
        } finally {
            setLoading(false);
        }
    };

    const updateAttendanceStatus = async (status, personelId) => {
        setUpdatingUserId(personelId);
        try {
            await api.post('/meetings/attendance/status', {
                meetingId: meeting.id,
                personelId: personelId,
                attendanceStatus: status
            });
            toast.success('Katılım durumu güncellendi');
            fetchMeeting();
        } catch (error) {
            toast.error(error.response?.data?.message || 'Güncelleme başarısız');
        } finally {
            setUpdatingUserId(null);
        }
    };

    const getStatusBadge = (status) => {
        const config = {
            'Planlandı': { icon: '📅', text: 'Planlandı', color: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300' },
            'Devam Ediyor': { icon: '🔄', text: 'Devam Ediyor', color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300' },
            'Tamamlandı': { icon: '✅', text: 'Tamamlandı', color: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
            'İptal': { icon: '❌', text: 'İptal', color: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300' }
        };
        const c = config[status] || config['Planlandı'];
        return <span className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium ${c.color}`}>
            {c.icon} {c.text}
        </span>;
    };

    const getAttendanceStatusBadge = (status) => {
        if (status === 'Katılıyorum') {
            return <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400">✅ Katılıyorum</span>;
        } else if (status === 'Katılamıyorum') {
            return <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400">❌ Katılamıyorum</span>;
        }
        return <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400">⏳ Beklemede</span>;
    };

    const formatDateTime = (date) => {
        return new Date(date).toLocaleString('tr-TR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    const formatDateShort = (date) => {
        return new Date(date).toLocaleDateString('tr-TR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    };

    const canEdit = isAdmin || meeting?.createdByPersonelId === Number(currentPersonelId);

    // Pagination hesaplamaları
    const attendees = meeting?.attendees || [];
    const totalItems = attendees.length;
    const totalPages = Math.ceil(totalItems / itemsPerPage);
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    const currentAttendees = attendees.slice(startIndex, endIndex);

    const goToPage = (page) => {
        if (page >= 1 && page <= totalPages) {
            setCurrentPage(page);
        }
    };

    if (loading) {
        return (
            <div className="flex justify-center items-center h-96">
                <div className="text-center">
                    <div className="animate-spin rounded-full h-16 w-16 border-4 border-indigo-500/20 border-t-indigo-600 mx-auto mb-4"></div>
                    <p className="text-gray-500 dark:text-gray-400">Toplantı bilgileri yükleniyor...</p>
                </div>
            </div>
        );
    }

    if (!meeting) {
        return (
            <div className="flex flex-col items-center justify-center h-96">
                <div className="text-6xl mb-4">📅</div>
                <p className="text-gray-500 dark:text-gray-400 text-lg">Toplantı bulunamadı</p>
                <Link to="/meetings" className="mt-4 text-indigo-600 hover:text-indigo-700 flex items-center gap-2">
                    ← Toplantılara dön
                </Link>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
            {/* Header */}
            <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 sticky top-0 z-10">
                <div className="max-w-7xl mx-auto px-6 py-4">
                    <div className="flex flex-wrap items-center justify-between gap-4">
                        <div className="flex items-center gap-4">
                            <Link to="/meetings" className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200">
                                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
                                </svg>
                            </Link>
                            <div>
                                <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{meeting.title}</h1>
                                <div className="flex items-center gap-2 mt-1">
                                    {getStatusBadge(meeting.status)}
                                    <span className="text-sm text-gray-500 dark:text-gray-400">
                                        Oluşturan: {meeting.createdByPersonelName || 'Sistem'} • {formatDateShort(meeting.createdAt)}
                                    </span>
                                </div>
                            </div>
                        </div>
                        {canEdit && (
                            <Link to={`/meetings/edit/${meeting.id}`} className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg transition-colors shadow-sm">
                                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                                </svg>
                                Düzenle
                            </Link>
                        )}
                    </div>
                </div>
            </div>

            {/* Content - Table Layout */}
            <div className="max-w-7xl mx-auto px-6 py-8">
                <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
                    <table className="w-full">
                        <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                            {/* Tarih & Zaman */}
                            <tr className="hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors">
                                <td className="px-6 py-4 w-48 align-top">
                                    <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400 font-medium">
                                        <span className="text-xl">⏰</span> Tarih & Zaman
                                    </div>
                                </td>
                                <td className="px-6 py-4">
                                    <div className="space-y-1">
                                        <div className="flex items-center gap-2 flex-wrap">
                                            <span className="font-semibold text-gray-900 dark:text-white">{formatDateTime(meeting.startTime)}</span>
                                            <span className="text-gray-400">→</span>
                                            <span className="font-semibold text-gray-900 dark:text-white">{formatDateTime(meeting.endTime)}</span>
                                        </div>
                                        <div className="text-sm text-gray-500 dark:text-gray-400">
                                            Süre: {Math.round((new Date(meeting.endTime) - new Date(meeting.startTime)) / (1000 * 60))} dakika
                                        </div>
                                    </div>
                                </td>
                            </tr>

                            {/* Konum */}
                            <tr className="hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors">
                                <td className="px-6 py-4 w-48 align-top">
                                    <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400 font-medium">
                                        <span className="text-xl">📍</span> Konum
                                    </div>
                                </td>
                                <td className="px-6 py-4">
                                    {meeting.location ? (
                                        <span className="text-gray-900 dark:text-white">{meeting.location}</span>
                                    ) : (
                                        <span className="text-gray-400 dark:text-gray-500 italic">Belirtilmemiş</span>
                                    )}
                                </td>
                            </tr>

                            {/* Toplantı Linki */}
                            <tr className="hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors">
                                <td className="px-6 py-4 w-48 align-top">
                                    <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400 font-medium">
                                        <span className="text-xl">🔗</span> Toplantı Linki
                                    </div>
                                </td>
                                <td className="px-6 py-4">
                                    {meeting.meetingLink ? (
                                        <a href={meeting.meetingLink} target="_blank" rel="noopener noreferrer" className="text-indigo-600 hover:text-indigo-700 dark:text-indigo-400 hover:underline break-all">
                                            {meeting.meetingLink}
                                        </a>
                                    ) : (
                                        <span className="text-gray-400 dark:text-gray-500 italic">Belirtilmemiş</span>
                                    )}
                                </td>
                            </tr>

                            {/* İlgili Müşteri */}
                            {meeting.customerName && (
                                <tr className="hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors">
                                    <td className="px-6 py-4 w-48 align-top">
                                        <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400 font-medium">
                                            <span className="text-xl">🏢</span> İlgili Müşteri
                                        </div>
                                    </td>
                                    <td className="px-6 py-4">
                                        <span className="text-gray-900 dark:text-white">{meeting.customerName}</span>
                                    </td>
                                </tr>
                            )}

                            {/* İlgili Lead */}
                            {meeting.leadName && (
                                <tr className="hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors">
                                    <td className="px-6 py-4 w-48 align-top">
                                        <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400 font-medium">
                                            <span className="text-xl">🎯</span> İlgili Lead
                                        </div>
                                    </td>
                                    <td className="px-6 py-4">
                                        <span className="text-gray-900 dark:text-white">{meeting.leadName}</span>
                                    </td>
                                </tr>
                            )}

                            {/* Açıklama */}
                            {meeting.description && (
                                <tr className="hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors">
                                    <td className="px-6 py-4 w-48 align-top">
                                        <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400 font-medium">
                                            <span className="text-xl">📝</span> Açıklama
                                        </div>
                                    </td>
                                    <td className="px-6 py-4">
                                        <p className="text-gray-700 dark:text-gray-300 whitespace-pre-wrap leading-relaxed">
                                            {meeting.description}
                                        </p>
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>

                {/* Katılımcılar Tablosu (Ayrı tablo) */}
                <div className="mt-8 bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
                    <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
                        <div className="flex items-center justify-between flex-wrap gap-3">
                            <div className="flex items-center gap-2">
                                <span className="text-xl">👥</span>
                                <h2 className="font-semibold text-gray-900 dark:text-white">Katılımcılar</h2>
                                <span className="px-2 py-0.5 text-xs bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-400 rounded-full">
                                    {totalItems} kişi
                                </span>
                            </div>
                            {totalItems > 0 && (
                                <select
                                    value={itemsPerPage}
                                    onChange={(e) => {
                                        setItemsPerPage(Number(e.target.value));
                                        setCurrentPage(1);
                                    }}
                                    className="text-sm border border-gray-300 dark:border-gray-600 rounded-lg px-2 py-1 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300"
                                >
                                    <option value={5}>5 göster</option>
                                    <option value={10}>10 göster</option>
                                    <option value={20}>20 göster</option>
                                    <option value={50}>50 göster</option>
                                </select>
                            )}
                        </div>
                    </div>

                    {attendees.length === 0 ? (
                        <div className="p-12 text-center text-gray-500 dark:text-gray-400">
                            <div className="text-5xl mb-3">👥</div>
                            <p>Henüz katılımcı eklenmemiş</p>
                        </div>
                    ) : (
                        <>
                            <div className="overflow-x-auto">
                                <table className="w-full">
                                    <thead className="bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-700">
                                        <tr>
                                            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">#</th>
                                            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Katılımcı</th>
                                            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                                            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlem</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                                        {currentAttendees.map((attendee, index) => {
                                            const isCurrentUser = Number(currentPersonelId) === attendee.personelId;
                                            const currentStatus = attendee.attendanceStatus;
                                            const isUpdating = updatingUserId === attendee.personelId;
                                            const rowNumber = startIndex + index + 1;
                                            
                                            return (
                                                <tr key={attendee.id} className={`hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors ${isCurrentUser ? 'bg-indigo-50/30 dark:bg-indigo-900/10' : ''}`}>
                                                    <td className="px-6 py-4 text-sm text-gray-500 dark:text-gray-400">
                                                        {rowNumber}
                                                    </td>
                                                    <td className="px-6 py-4">
                                                        <div className="flex items-center gap-3">
                                                            <div className="relative">
                                                                <div className="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-white font-semibold text-sm shadow-sm">
                                                                    {attendee.personelName?.charAt(0) || '?'}
                                                                </div>
                                                                {isCurrentUser && (
                                                                    <div className="absolute -bottom-0.5 -right-0.5 w-3 h-3 bg-green-500 rounded-full border-2 border-white dark:border-gray-800"></div>
                                                                )}
                                                            </div>
                                                            <div>
                                                                <span className="font-medium text-gray-900 dark:text-white">
                                                                    {attendee.personelName}
                                                                </span>
                                                                {isCurrentUser && (
                                                                    <span className="ml-2 text-xs text-indigo-600 dark:text-indigo-400">(Siz)</span>
                                                                )}
                                                            </div>
                                                        </div>
                                                    </td>
                                                    <td className="px-6 py-4">
                                                        {getAttendanceStatusBadge(currentStatus)}
                                                    </td>
                                                    <td className="px-6 py-4">
                                                        {isCurrentUser ? (
                                                            <div className="flex gap-2">
                                                                <button
                                                                    onClick={() => updateAttendanceStatus('Katılıyorum', attendee.personelId)}
                                                                    disabled={isUpdating || currentStatus === 'Katılıyorum'}
                                                                    className={`px-3 py-1.5 text-sm rounded-lg transition-all duration-200 flex items-center gap-1 ${
                                                                        currentStatus === 'Katılıyorum'
                                                                            ? 'bg-green-600 text-white cursor-default'
                                                                            : 'bg-green-100 text-green-700 hover:bg-green-600 hover:text-white dark:bg-green-900/30 dark:text-green-400 dark:hover:bg-green-600'
                                                                    }`}
                                                                >
                                                                    {isUpdating ? (
                                                                        <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                                                                    ) : (
                                                                        <>✅ Katıl</>
                                                                    )}
                                                                </button>
                                                                <button
                                                                    onClick={() => updateAttendanceStatus('Katılamıyorum', attendee.personelId)}
                                                                    disabled={isUpdating || currentStatus === 'Katılamıyorum'}
                                                                    className={`px-3 py-1.5 text-sm rounded-lg transition-all duration-200 flex items-center gap-1 ${
                                                                        currentStatus === 'Katılamıyorum'
                                                                            ? 'bg-red-600 text-white cursor-default'
                                                                            : 'bg-red-100 text-red-700 hover:bg-red-600 hover:text-white dark:bg-red-900/30 dark:text-red-400 dark:hover:bg-red-600'
                                                                    }`}
                                                                >
                                                                    {isUpdating ? (
                                                                        <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                                                                    ) : (
                                                                        <>❌ Katılma</>
                                                                    )}
                                                                </button>
                                                            </div>
                                                        ) : (
                                                            <span className="text-sm text-gray-400 dark:text-gray-500">-</span>
                                                        )}
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                    </tbody>
                                </table>
                            </div>

                            {/* Pagination */}
                            {totalPages > 1 && (
                                <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between flex-wrap gap-3 bg-gray-50 dark:bg-gray-800/50">
                                    <div className="text-sm text-gray-500 dark:text-gray-400">
                                        {startIndex + 1} - {Math.min(endIndex, totalItems)} / {totalItems} kayıt
                                    </div>
                                    <div className="flex gap-1">
                                        <button
                                            onClick={() => goToPage(currentPage - 1)}
                                            disabled={currentPage === 1}
                                            className="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-white dark:hover:bg-gray-700 transition-colors"
                                        >
                                            ← Önceki
                                        </button>
                                        {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                                            let pageNum;
                                            if (totalPages <= 5) {
                                                pageNum = i + 1;
                                            } else if (currentPage <= 3) {
                                                pageNum = i + 1;
                                            } else if (currentPage >= totalPages - 2) {
                                                pageNum = totalPages - 4 + i;
                                            } else {
                                                pageNum = currentPage - 2 + i;
                                            }
                                            
                                            return (
                                                <button
                                                    key={pageNum}
                                                    onClick={() => goToPage(pageNum)}
                                                    className={`px-3 py-1.5 text-sm rounded-lg transition-colors ${
                                                        currentPage === pageNum
                                                            ? 'bg-indigo-600 text-white'
                                                            : 'border border-gray-300 dark:border-gray-600 hover:bg-white dark:hover:bg-gray-700'
                                                    }`}
                                                >
                                                    {pageNum}
                                                </button>
                                            );
                                        })}
                                        <button
                                            onClick={() => goToPage(currentPage + 1)}
                                            disabled={currentPage === totalPages}
                                            className="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-white dark:hover:bg-gray-700 transition-colors"
                                        >
                                            Sonraki →
                                        </button>
                                    </div>
                                </div>
                            )}
                        </>
                    )}
                </div>
            </div>
        </div>
    );
}