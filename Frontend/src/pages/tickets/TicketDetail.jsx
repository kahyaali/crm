// src/pages/tickets/TicketDetail.jsx
import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useSignalR } from '../../contexts/SignalRContext';
import ticketApi from '../../services/ticketApi';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function TicketDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { isConnected } = useSignalR();
  const [ticket, setTicket] = useState(null);
  const [loading, setLoading] = useState(true);
  const [newComment, setNewComment] = useState('');
  const [isInternal, setIsInternal] = useState(false);
  const [isSolution, setIsSolution] = useState(false);
  const [updating, setUpdating] = useState(false);
  const [statusList, setStatusList] = useState([]);
  const [priorityList, setPriorityList] = useState([]);
  const [editMode, setEditMode] = useState(false);
  const [editForm, setEditForm] = useState({
    subject: '',
    description: '',
    status: '',
    priority: '',
    category: ''
  });
  
  const commentsEndRef = useRef(null);
  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
  const isTicketClosed = ticket?.status === 'Closed' || ticket?.status === 'Resolved';

  useEffect(() => {
    fetchTicket();
    fetchLists();
  }, [id]);

  useEffect(() => {
    scrollToBottom();
  }, [ticket?.comments]);

  const scrollToBottom = () => {
    commentsEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const fetchTicket = async () => {
    try {
      setLoading(true);
      const data = await ticketApi.getById(id);
      setTicket(data);
      setEditForm({
        subject: data.subject,
        description: data.description || '',
        status: data.status,
        priority: data.priority || '',
        category: data.category || ''
      });
    } catch (error) {
      toast.error('Ticket detayı yüklenemedi');
      navigate('/tickets');
    } finally {
      setLoading(false);
    }
  };

  const fetchLists = async () => {
    try {
      const [status, priority] = await Promise.all([
        ticketApi.getStatusList(),
        ticketApi.getPriorityList()
      ]);
      setStatusList(status);
      setPriorityList(priority);
    } catch (error) {
      console.error('Listeler yüklenemedi:', error);
    }
  };

  const handleAddComment = async (e) => {
    e.preventDefault();
    if (!newComment.trim()) {
      toast.error('Yorum boş olamaz');
      return;
    }

    if (isTicketClosed) {
      toast.error('Bu ticket kapatılmış, yorum eklenemez');
      return;
    }

    try {
      setUpdating(true);
      const comment = await ticketApi.addComment(id, {
        comment: newComment,
        isInternal,
        isSolution
      });
      
      setTicket(prev => ({
        ...prev,
        comments: [comment, ...(prev.comments || [])],
        status: isSolution ? 'Closed' : prev.status,
        commentCount: (prev.commentCount || 0) + 1
      }));
      
      setNewComment('');
      setIsInternal(false);
      setIsSolution(false);
      toast.success(isSolution ? '✅ Çözüm notu eklendi, ticket kapatıldı' : 'Yorum eklendi');
    } catch (error) {
      toast.error('Yorum eklenemedi');
    } finally {
      setUpdating(false);
    }
  };

  const handleUpdateTicket = async () => {
    const updateData = {
      id: ticket.id,
      subject: editForm.subject,
      description: editForm.description || null,
      status: editForm.status,
      priority: editForm.priority || null,
      category: editForm.category || null
    };
    
    Object.keys(updateData).forEach(key => {
      if (updateData[key] === null || updateData[key] === '') {
        delete updateData[key];
      }
    });
    
    console.log('Gönderilecek veri:', updateData);
    
    try {
      setUpdating(true);
      const updated = await ticketApi.update(id, updateData);
      setTicket(prev => ({ ...prev, ...updated }));
      setEditMode(false);
      toast.success('Ticket güncellendi');
    } catch (error) {
      console.error('Güncelleme hatası:', error.response?.data);
      toast.error(error.response?.data?.message || 'Güncelleme başarısız');
    } finally {
      setUpdating(false);
    }
  };

  const handleDelete = async () => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${ticket?.ticketNumber} numaralı talebi silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal',
      background: document.documentElement.classList.contains('dark') ? '#1f2937' : '#fff',
      color: document.documentElement.classList.contains('dark') ? '#f3f4f6' : '#1f2937'
    });

    if (result.isConfirmed) {
      try {
        await ticketApi.delete(id);
        toast.success('Ticket silindi');
        navigate('/tickets');
      } catch (error) {
        toast.error('Silme başarısız');
      }
    }
  };

  // 🔥 YORUM SİLME FONKSİYONU
  const handleDeleteComment = async (commentId) => {
    const result = await Swal.fire({
      title: 'Yorumu Sil',
      text: 'Bu yorumu silmek istediğinize emin misiniz?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal',
      background: document.documentElement.classList.contains('dark') ? '#1f2937' : '#fff',
      color: document.documentElement.classList.contains('dark') ? '#f3f4f6' : '#1f2937'
    });

    if (result.isConfirmed) {
      try {
        await api.delete(`/tickets/comments/${commentId}`);
        setTicket(prev => ({
          ...prev,
          comments: prev.comments.filter(c => c.id !== commentId),
          commentCount: (prev.commentCount || 0) - 1
        }));
        toast.success('Yorum silindi');
      } catch (error) {
        toast.error('Yorum silinemedi');
      }
    }
  };

  const getPriorityBadge = (priority) => {
    const config = {
      'Low': { icon: '🟢', text: 'Düşük', color: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
      'Medium': { icon: '🟡', text: 'Orta', color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300' },
      'High': { icon: '🟠', text: 'Yüksek', color: 'bg-orange-100 text-orange-800 dark:bg-orange-900/40 dark:text-orange-300' },
      'Critical': { icon: '🔴', text: 'Acil', color: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300' }
    };
    const c = config[priority] || config['Medium'];
    return <span className={`px-2 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
  };

  const getStatusBadge = (status) => {
    const config = {
      'Open': { icon: '🟢', text: 'Açık', color: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
      'InProgress': { icon: '🔵', text: 'İşlemde', color: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300' },
      'OnHold': { icon: '🟡', text: 'Beklemede', color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300' },
      'Resolved': { icon: '✅', text: 'Çözüldü', color: 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-300' },
      'Closed': { icon: '🔒', text: 'Kapandı', color: 'bg-gray-100 text-gray-800 dark:bg-gray-800/40 dark:text-gray-300' }
    };
    const c = config[status] || config['Open'];
    return <span className={`px-2 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
      </div>
    );
  }

  if (!ticket) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500 dark:text-gray-400">Ticket bulunamadı</p>
        <Link to="/tickets" className="mt-4 inline-block text-indigo-600 hover:text-indigo-700">
          Ticket listesine dön
        </Link>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-5xl">
      {/* Header */}
      <div className="flex justify-between items-start mb-6">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
              {ticket.ticketNumber}
            </h1>
            {getPriorityBadge(ticket.priority)}
            {getStatusBadge(ticket.status)}
          </div>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Oluşturan: {ticket.createdByPersonelName || '-'} | 
            Oluşturma: {new Date(ticket.createdAt).toLocaleString('tr-TR')} |
            Müşteri: {ticket.customerName}
          </p>
        </div>
        <div className="flex gap-2">
          <Link
            to="/tickets"
            className="px-4 py-2 bg-gray-500 hover:bg-gray-600 text-white rounded-lg transition-colors text-sm"
          >
            ← Geri
          </Link>
          {isAdmin && (
            <>
              <button
                onClick={() => setEditMode(!editMode)}
                className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg transition-colors text-sm"
              >
                {editMode ? 'İptal' : '✏️ Düzenle'}
              </button>
              <button
                onClick={handleDelete}
                className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg transition-colors text-sm"
              >
                🗑️ Sil
              </button>
            </>
          )}
        </div>
      </div>

      {/* Edit Mode */}
      {editMode ? (
        <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-6 mb-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Ticket Düzenle</h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Konu</label>
              <input
                type="text"
                value={editForm.subject}
                onChange={(e) => setEditForm({ ...editForm, subject: e.target.value })}
                className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Açıklama</label>
              <textarea
                rows="3"
                value={editForm.description}
                onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100"
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Durum</label>
                <select
                  value={editForm.status}
                  onChange={(e) => setEditForm({ ...editForm, status: e.target.value })}
                  className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100"
                >
                  {statusList.map(opt => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Öncelik</label>
                <select
                  value={editForm.priority}
                  onChange={(e) => setEditForm({ ...editForm, priority: e.target.value })}
                  className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100"
                >
                  <option value="">Seçiniz</option>
                  {priorityList.map(opt => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="flex justify-end gap-3">
              <button
                onClick={() => setEditMode(false)}
                className="px-4 py-2 bg-gray-500 hover:bg-gray-600 text-white rounded-lg"
              >
                İptal
              </button>
              <button
                onClick={handleUpdateTicket}
                disabled={updating}
                className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg disabled:opacity-50"
              >
                {updating ? 'Kaydediliyor...' : 'Kaydet'}
              </button>
            </div>
          </div>
        </div>
      ) : (
        ticket.description && (
          <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-6 mb-6">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Açıklama</h2>
            <p className="text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{ticket.description}</p>
          </div>
        )
      )}

      {/* Comments Section */}
      <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 overflow-hidden mb-6">
        <div className="px-6 py-4 bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
            Yorumlar ({ticket.comments?.length || 0})
          </h2>
          {isConnected && (
            <span className="text-xs text-green-600 dark:text-green-400 ml-2">🟢 Canlı bağlantı aktif</span>
          )}
        </div>

        {/* Add Comment Form - Sadece ticket kapalı değilse göster */}
        {!isTicketClosed ? (
          <div className="p-6 border-b border-gray-200 dark:border-gray-700">
            <form onSubmit={handleAddComment}>
              <textarea
                rows="3"
                value={newComment}
                onChange={(e) => setNewComment(e.target.value)}
                placeholder="Yorumunuzu yazın..."
                className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500"
              />
              <div className="flex flex-wrap gap-4 mt-3">
                <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                  <input
                    type="checkbox"
                    checked={isInternal}
                    onChange={(e) => setIsInternal(e.target.checked)}
                    className="rounded border-gray-300 dark:border-gray-600"
                  />
                  <span>🔒 Sadece personel görsün</span>
                </label>
                <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                  <input
                    type="checkbox"
                    checked={isSolution}
                    onChange={(e) => setIsSolution(e.target.checked)}
                    className="rounded border-gray-300 dark:border-gray-600"
                  />
                  <span>✅ Çözüm notu (Ticket'ı kapatır)</span>
                </label>
                <button
                  type="submit"
                  disabled={updating || !newComment.trim()}
                  className="ml-auto px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 text-white rounded-lg text-sm transition-colors"
                >
                  Yorum Gönder
                </button>
              </div>
            </form>
          </div>
        ) : (
          <div className="p-6 border-b border-gray-200 dark:border-gray-700 text-center text-gray-500 dark:text-gray-400">
            🔒 Bu ticket kapatılmış, yeni yorum eklenemez.
          </div>
        )}

        {/* Comments List */}
        <div className="divide-y divide-gray-200 dark:divide-gray-800 max-h-[500px] overflow-y-auto">
          {ticket.comments?.length === 0 ? (
            <div className="p-6 text-center text-gray-500 dark:text-gray-400">
              Henüz yorum yapılmamış
            </div>
          ) : (
            [...(ticket.comments || [])].reverse().map((comment) => (
              <div key={comment.id} className="p-4 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition-colors">
                <div className="flex justify-between items-start mb-2">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-gray-900 dark:text-white">
                      {comment.personelName || 'Sistem'}
                    </span>
                    {comment.isInternal && (
                      <span className="text-xs bg-yellow-100 dark:bg-yellow-900/40 text-yellow-800 dark:text-yellow-300 px-2 py-0.5 rounded">
                        🔒 Personel Notu
                      </span>
                    )}
                    {comment.isSolution && (
                      <span className="text-xs bg-green-100 dark:bg-green-900/40 text-green-800 dark:text-green-300 px-2 py-0.5 rounded">
                        ✅ Çözüm Notu
                      </span>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-gray-500 dark:text-gray-400">
                      {new Date(comment.createdAt).toLocaleString('tr-TR')}
                    </span>
                    {isAdmin && (
                      <button
                        onClick={() => handleDeleteComment(comment.id)}
                        className="text-red-500 hover:text-red-700 text-sm transition-colors"
                        title="Yorumu Sil"
                      >
                        🗑️
                      </button>
                    )}
                  </div>
                </div>
                <p className="text-gray-700 dark:text-gray-300 whitespace-pre-wrap">
                  {comment.comment}
                </p>
              </div>
            ))
          )}
          <div ref={commentsEndRef} />
        </div>
      </div>
    </div>
  );
}