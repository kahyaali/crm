// src/components/NotificationPanel.jsx
import { useState, useEffect, useRef } from 'react';
import { useSignalR } from '../contexts/SignalRContext';
import api from '../services/api';
import toast from 'react-hot-toast';

export default function NotificationPanel() {
  const { isConnected } = useSignalR();
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [showPanel, setShowPanel] = useState(false);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const panelRef = useRef(null);

  useEffect(() => {
    fetchNotifications();
    fetchUnreadCount();

    // Click outside to close
    const handleClickOutside = (event) => {
      if (panelRef.current && !panelRef.current.contains(event.target)) {
        setShowPanel(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  useEffect(() => {
    if (showPanel) {
      fetchNotifications();
    }
  }, [page, showPanel]);

  const fetchNotifications = async () => {
    try {
      setLoading(true);
      const response = await api.get('/notifications', {
        params: { page, pageSize: 20 }
      });
      setNotifications(response.data.data || []);
      setTotalPages(response.data.totalPages || 1);
    } catch (error) {
      console.error('Bildirimler yüklenemedi:', error);
    } finally {
      setLoading(false);
    }
  };

  const fetchUnreadCount = async () => {
    try {
      const response = await api.get('/notifications/unread-count');
      setUnreadCount(response.data.unreadCount || 0);
    } catch (error) {
      console.error('Okunmamış sayısı alınamadı:', error);
    }
  };

  const markAsRead = async (id) => {
    try {
      await api.post(`/notifications/${id}/mark-as-read`);
      setNotifications(prev => prev.map(n => 
        n.id === id ? { ...n, isRead: true } : n
      ));
      setUnreadCount(prev => Math.max(0, prev - 1));
    } catch (error) {
      toast.error('Bildirim okundu işaretlenemedi');
    }
  };

  const markAllAsRead = async () => {
    try {
      await api.post('/notifications/mark-all-as-read');
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
      setUnreadCount(0);
      toast.success('Tüm bildirimler okundu olarak işaretlendi');
    } catch (error) {
      toast.error('İşlem başarısız');
    }
  };

  const deleteNotification = async (id) => {
    try {
      await api.delete(`/notifications/${id}`);
      setNotifications(prev => prev.filter(n => n.id !== id));
      if (!notifications.find(n => n.id === id)?.isRead) {
        setUnreadCount(prev => Math.max(0, prev - 1));
      }
      toast.success('Bildirim silindi');
    } catch (error) {
      toast.error('Silme başarısız');
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
    <div className="relative" ref={panelRef}>
      {/* Bildirim Butonu */}
      <button
        onClick={() => setShowPanel(!showPanel)}
        className="relative p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
      >
        <svg className="w-6 h-6 text-gray-600 dark:text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
        </svg>
        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
        {isConnected && (
          <span className="absolute bottom-0 right-0 w-2 h-2 bg-green-500 rounded-full"></span>
        )}
      </button>

      {/* Bildirim Paneli */}
      {showPanel && (
        <div className="absolute right-0 mt-2 w-96 bg-white dark:bg-gray-800 rounded-xl shadow-2xl border border-gray-200 dark:border-gray-700 z-50 overflow-hidden">
          {/* Header */}
          <div className="flex justify-between items-center px-4 py-3 border-b border-gray-200 dark:border-gray-700">
            <h3 className="font-semibold text-gray-900 dark:text-white">Bildirimler</h3>
            {unreadCount > 0 && (
              <button
                onClick={markAllAsRead}
                className="text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400"
              >
                Tümünü Okundu İşaretle
              </button>
            )}
          </div>

          {/* Liste */}
          <div className="max-h-96 overflow-y-auto">
            {loading && notifications.length === 0 ? (
              <div className="flex justify-center items-center py-8">
                <div className="animate-spin rounded-full h-6 w-6 border-2 border-indigo-500/20 border-t-indigo-600"></div>
              </div>
            ) : notifications.length === 0 ? (
              <div className="text-center py-8 text-gray-500 dark:text-gray-400">
                <span className="text-4xl block mb-2">🔔</span>
                <p>Henüz bildirim yok</p>
              </div>
            ) : (
              notifications.map((notification) => (
                <div
                  key={notification.id}
                  className={`p-4 border-b border-gray-100 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors cursor-pointer ${
                    !notification.isRead ? 'bg-blue-50 dark:bg-blue-900/20' : ''
                  }`}
                  onClick={() => markAsRead(notification.id)}
                >
                  <div className="flex gap-3">
                    <div className="flex-shrink-0 text-2xl">
                      {getTypeIcon(notification.type)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex justify-between items-start">
                        <p className="text-sm font-medium text-gray-900 dark:text-white">
                          {notification.title}
                        </p>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            deleteNotification(notification.id);
                          }}
                          className="text-gray-400 hover:text-red-500 transition-colors"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                          </svg>
                        </button>
                      </div>
                      <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                        {notification.message}
                      </p>
                      <p className="text-xs text-gray-400 mt-1">
                        {new Date(notification.createdAt).toLocaleString('tr-TR')}
                      </p>
                      {!notification.isRead && (
                        <span className="inline-block mt-1 w-2 h-2 bg-blue-500 rounded-full"></span>
                      )}
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>

          {/* Footer */}
          {totalPages > 1 && (
            <div className="flex justify-between items-center px-4 py-2 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="text-xs text-gray-500 disabled:opacity-50"
              >
                ← Önceki
              </button>
              <span className="text-xs text-gray-500">Sayfa {page} / {totalPages}</span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                className="text-xs text-gray-500 disabled:opacity-50"
              >
                Sonraki →
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}