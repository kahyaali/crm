// src/pages/NotificationsPage.jsx
import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import api from '../services/api';
import toast from 'react-hot-toast';

export default function NotificationsPage() {
  const { user } = useAuth();
  const [notifications, setNotifications] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [filter, setFilter] = useState('all'); // all, unread, read
  const pageSize = 20;

  useEffect(() => {
    fetchNotifications();
  }, [page, filter]);

  const fetchNotifications = async () => {
    try {
      setLoading(true);
      const params = {
        page,
        pageSize,
        isRead: filter === 'all' ? null : filter === 'unread' ? false : true
      };
      const response = await api.get('/notifications', { params });
      setNotifications(response.data.data || []);
      setTotalCount(response.data.totalCount || 0);
      setTotalPages(response.data.totalPages || 1);
    } catch (error) {
      toast.error('Bildirimler yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const markAsRead = async (id) => {
    try {
      await api.post(`/notifications/${id}/mark-as-read`);
      setNotifications(prev => prev.map(n => 
        n.id === id ? { ...n, isRead: true } : n
      ));
    } catch (error) {
      toast.error('İşlem başarısız');
    }
  };

  const markAllAsRead = async () => {
    try {
      await api.post('/notifications/mark-all-as-read');
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
      toast.success('Tüm bildirimler okundu');
    } catch (error) {
      toast.error('İşlem başarısız');
    }
  };

  const deleteNotification = async (id) => {
    try {
      await api.delete(`/notifications/${id}`);
      setNotifications(prev => prev.filter(n => n.id !== id));
      toast.success('Bildirim silindi');
    } catch (error) {
      toast.error('Silme başarısız');
    }
  };

  const deleteAllRead = async () => {
    try {
      await api.delete('/notifications/delete-all-read');
      setNotifications(prev => prev.filter(n => !n.isRead));
      toast.success('Okunmuş bildirimler silindi');
    } catch (error) {
      toast.error('İşlem başarısız');
    }
  };

  const getTypeIcon = (type) => {
    const icons = {
      'Ticket': '🎫',
      'Task': '✅',
      'Meeting': '📅',
      'Lead': '🎯',
      'Order': '🛒',
      'System': '⚙️'
    };
    return icons[type] || '🔔';
  };

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Bildirimler</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} bildirim</p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={markAllAsRead}
            className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg text-sm"
          >
            Tümünü Okundu İşaretle
          </button>
          <button
            onClick={deleteAllRead}
            className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg text-sm"
          >
            Okunmuşları Sil
          </button>
        </div>
      </div>

      {/* Filtreler */}
      <div className="flex gap-2 mb-6">
        <button
          onClick={() => { setFilter('all'); setPage(1); }}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
            filter === 'all' 
              ? 'bg-indigo-600 text-white' 
              : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
          }`}
        >
          Tümü
        </button>
        <button
          onClick={() => { setFilter('unread'); setPage(1); }}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
            filter === 'unread' 
              ? 'bg-indigo-600 text-white' 
              : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
          }`}
        >
          Okunmamış
        </button>
        <button
          onClick={() => { setFilter('read'); setPage(1); }}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
            filter === 'read' 
              ? 'bg-indigo-600 text-white' 
              : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
          }`}
        >
          Okunmuş
        </button>
      </div>

      {/* Liste */}
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-800 overflow-hidden">
        {loading ? (
          <div className="flex justify-center items-center py-12">
            <div className="animate-spin rounded-full h-8 w-8 border-4 border-indigo-500/20 border-t-indigo-600"></div>
          </div>
        ) : notifications.length === 0 ? (
          <div className="text-center py-12">
            <span className="text-5xl block mb-3">🔔</span>
            <p className="text-gray-500 dark:text-gray-400">Henüz bildirim yok</p>
          </div>
        ) : (
          <div className="divide-y divide-gray-200 dark:divide-gray-700">
            {notifications.map((notification) => (
              <div
                key={notification.id}
                className={`p-4 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors cursor-pointer ${
                  !notification.isRead ? 'bg-blue-50 dark:bg-blue-900/20' : ''
                }`}
                onClick={() => !notification.isRead && markAsRead(notification.id)}
              >
                <div className="flex gap-4">
                  <div className="flex-shrink-0 text-3xl">
                    {getTypeIcon(notification.type)}
                  </div>
                  <div className="flex-1">
                    <div className="flex justify-between items-start">
                      <div>
                        <h3 className="font-semibold text-gray-900 dark:text-white">
                          {notification.title}
                        </h3>
                        <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                          {notification.message}
                        </p>
                        <div className="flex gap-3 mt-2 text-xs text-gray-500 dark:text-gray-500">
                          <span>{notification.personelName || 'Sistem'}</span>
                          <span>{new Date(notification.createdAt).toLocaleString('tr-TR')}</span>
                          {notification.type && (
                            <span className="px-2 py-0.5 bg-gray-100 dark:bg-gray-700 rounded">
                              {notification.type}
                            </span>
                          )}
                        </div>
                      </div>
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          deleteNotification(notification.id);
                        }}
                        className="text-gray-400 hover:text-red-500 transition-colors"
                      >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex justify-between items-center px-4 py-3 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
            <div className="text-sm text-gray-500 dark:text-gray-400">
              Toplam {totalCount} bildirim
            </div>
            <div className="flex gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              >
                ◀
              </button>
              <span className="px-3 py-1 text-sm text-gray-700 dark:text-gray-300">
                Sayfa {page} / {totalPages}
              </span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                className="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              >
                ▶
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}