// src/pages/tasks/TaskDetail.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function TaskDetail() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [task, setTask] = useState(null);
    const [loading, setLoading] = useState(true);

    const isCreator = task?.createdByPersonelId === user?.personelId;
    const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
    const canEdit = isAdmin || isCreator;

    useEffect(() => {
        fetchTask();
    }, [id]);

    const fetchTask = async () => {
        try {
            setLoading(true);
            const response = await api.get(`/tasks/${id}`);
            setTask(response.data);
        } catch (error) {
            toast.error('Görev detayı yüklenemedi');
            navigate('/tasks');
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

    const getStatusBadge = (status) => {
        const config = {
            'Yeni': { icon: '🆕', text: 'Yeni', color: 'bg-blue-100 text-blue-700 dark:bg-blue-900/50 dark:text-blue-300' },
            'Devam Ediyor': { icon: '🔄', text: 'Devam Ediyor', color: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/50 dark:text-yellow-300' },
            'Tamamlandı': { icon: '✅', text: 'Tamamlandı', color: 'bg-green-100 text-green-700 dark:bg-green-900/50 dark:text-green-300' },
            'İptal': { icon: '🚫', text: 'İptal', color: 'bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-400' }
        };
        const c = config[status] || config['Yeni'];
        return (
            <span className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium ${c.color}`}>
                {c.icon} {c.text}
            </span>
        );
    };

    const getPriorityBadge = (priority) => {
        const config = {
            'Düşük': { icon: '🟢', text: 'Düşük', color: 'bg-green-100 text-green-700 dark:bg-green-900/50 dark:text-green-300' },
            'Orta': { icon: '🟡', text: 'Orta', color: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/50 dark:text-yellow-300' },
            'Yüksek': { icon: '🟠', text: 'Yüksek', color: 'bg-orange-100 text-orange-700 dark:bg-orange-900/50 dark:text-orange-300' },
            'Acil': { icon: '🔴', text: 'Acil', color: 'bg-red-100 text-red-700 dark:bg-red-900/50 dark:text-red-300' }
        };
        const c = config[priority] || config['Orta'];
        return (
            <span className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium ${c.color}`}>
                {c.icon} {c.text}
            </span>
        );
    };

  const handleComplete = async () => {
    const result = await Swal.fire({
        title: 'Görevi Tamamla',
        text: 'Bu görevi tamamlandı olarak işaretlemek istediğinize emin misiniz?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#22c55e',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Evet, Tamamla',
        cancelButtonText: 'İptal',
        background: document.documentElement.classList.contains('dark') ? '#1f2937' : '#ffffff',
        color: document.documentElement.classList.contains('dark') ? '#f3f4f6' : '#111827',
    });

    if (result.isConfirmed) {
        try {
            // Mevcut task verilerini al ve sadece status'u güncelle
            const updateData = {
                id: Number(id),
                title: task.title,
                description: task.description || null,
                assignedToPersonelId: task.assignedToPersonelId || null,
                relatedToCustomerId: task.relatedToCustomerId || null,
                relatedToLeadId: task.relatedToLeadId || null,
                relatedToOpportunityId: task.relatedToOpportunityId || null,
                status: 'Tamamlandı',
                priority: task.priority || 'Orta',
                dueDate: task.dueDate || null
            };

            console.log('📤 Gönderilen veri:', updateData);

            const response = await api.put(`/tasks/${id}`, updateData);
            console.log('✅ Başarılı yanıt:', response.data);
            
            toast.success('✅ Görev tamamlandı olarak işaretlendi');
            fetchTask(); // Sayfayı yenile
        } catch (error) {
            console.error('❌ Hata detayı:', {
                status: error.response?.status,
                data: error.response?.data,
                config: error.config
            });
            
            // Detaylı hata mesajı
            if (error.response?.data?.errors) {
                const errors = error.response.data.errors;
                Object.values(errors).flat().forEach(err => toast.error(err));
            } else if (error.response?.data?.message) {
                toast.error(error.response.data.message);
            } else {
                toast.error('Görev tamamlanamadı. Lütfen tekrar deneyin.');
            }
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

    if (!task) return null;

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-blue-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 py-8">
            <div className="max-w-6xl mx-auto px-6">
                {/* Header */}
                <div className="relative bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-2xl overflow-hidden mb-8">
                    <div className="relative px-6 py-8">
                        <div className="flex flex-wrap justify-between items-start gap-4">
                            <div>
                                <Link to="/tasks" className="inline-flex items-center gap-2 text-white/80 hover:text-white mb-4">
                                    ← Tüm Görevler
                                </Link>
                                <h1 className="text-3xl font-bold">{task.title}</h1>
                                <div className="flex flex-wrap gap-3 mt-2">
                                    {getStatusBadge(task.status)}
                                    {getPriorityBadge(task.priority)}
                                    <span className="text-white/80 text-sm">
                                        Oluşturan: {task.createdByPersonelName || 'Sistem'} • {formatDate(task.createdAt)}
                                    </span>
                                </div>
                            </div>
                            <div className="flex gap-3 flex-wrap">
                                {canEdit && task.status !== 'Tamamlandı' && task.status !== 'İptal' && (
                                    <>
                                        <Link to={`/tasks/edit/${task.id}`} className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 backdrop-blur-sm rounded-xl hover:bg-white/30">
                                            ✏️ Düzenle
                                        </Link>
                                        <button onClick={handleComplete} className="inline-flex items-center gap-2 px-4 py-2 bg-green-500/80 backdrop-blur-sm rounded-xl hover:bg-green-600">
                                            ✅ Tamamla
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
                                    <span className="text-xl">📋</span> Görev Bilgileri
                                </h2>
                            </div>
                            <div className="p-6 space-y-4">
                                <div>
                                    <p className="text-xs text-gray-500 dark:text-gray-400">Açıklama</p>
                                    <p className="text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{task.description || '-'}</p>
                                </div>
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">Atanan Personel</p>
                                        <p className="font-medium text-gray-900 dark:text-white">{task.assignedToPersonelName || '-'}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">Bitiş Tarihi</p>
                                        <p className="font-medium text-gray-900 dark:text-white">{formatDate(task.dueDate)}</p>
                                    </div>
                                </div>
                                <div>
                                    <p className="text-xs text-gray-500 dark:text-gray-400">Tamamlanma Tarihi</p>
                                    <p className="font-medium text-gray-900 dark:text-white">{formatDate(task.completedAt) || '-'}</p>
                                </div>
                            </div>
                        </div>

                        {/* İlişkili Kayıtlar */}
                        {(task.relatedToCustomerName || task.relatedToLeadName || task.relatedToOpportunityName) && (
                            <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg border border-gray-100 dark:border-gray-700 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-purple-50 to-pink-50 dark:from-purple-900/20 dark:to-pink-900/20 border-b border-gray-100 dark:border-gray-700">
                                    <h2 className="text-lg font-semibold flex items-center gap-2 text-gray-900 dark:text-white">
                                        <span className="text-xl">🔗</span> İlişkili Kayıtlar
                                    </h2>
                                </div>
                                <div className="p-6 space-y-3">
                                    {task.relatedToCustomerName && (
                                        <div className="flex items-center gap-2">
                                            <span className="text-lg">🏢</span>
                                            <span className="text-gray-700 dark:text-gray-300">Müşteri: <strong>{task.relatedToCustomerName}</strong></span>
                                        </div>
                                    )}
                                    {task.relatedToLeadName && (
                                        <div className="flex items-center gap-2">
                                            <span className="text-lg">🎯</span>
                                            <span className="text-gray-700 dark:text-gray-300">Lead: <strong>{task.relatedToLeadName}</strong></span>
                                        </div>
                                    )}
                                    {task.relatedToOpportunityName && (
                                        <div className="flex items-center gap-2">
                                            <span className="text-lg">💼</span>
                                            <span className="text-gray-700 dark:text-gray-300">Fırsat: <strong>{task.relatedToOpportunityName}</strong></span>
                                        </div>
                                    )}
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
                                    <span className="text-gray-600 dark:text-gray-400">Durum:</span>
                                    {getStatusBadge(task.status)}
                                </div>
                                <div className="flex justify-between items-center pb-2 border-b border-gray-100 dark:border-gray-700">
                                    <span className="text-gray-600 dark:text-gray-400">Öncelik:</span>
                                    {getPriorityBadge(task.priority)}
                                </div>
                                <div className="flex justify-between items-center pb-2 border-b border-gray-100 dark:border-gray-700">
                                    <span className="text-gray-600 dark:text-gray-400">Atanan:</span>
                                    <span className="font-medium text-gray-900 dark:text-white">{task.assignedToPersonelName || '-'}</span>
                                </div>
                                <div className="flex justify-between items-center">
                                    <span className="text-gray-600 dark:text-gray-400">Oluşturan:</span>
                                    <span className="font-medium text-gray-900 dark:text-white">{task.createdByPersonelName || 'Sistem'}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}