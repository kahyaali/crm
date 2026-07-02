// src/pages/tickets/Tickets.jsx
import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useSignalR } from '../../contexts/SignalRContext';
import ticketApi from '../../services/ticketApi';
import toast from 'react-hot-toast';

export default function Tickets() {
  const { user } = useAuth();
  const { isConnected, refreshSignal } = useSignalR();
  const [tickets, setTickets] = useState([]);
  const [loading, setLoading] = useState(true);
  
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 10;
  
  const [filters, setFilters] = useState({
    search: '',
    status: '',
    priority: '',
    category: ''
  });
  
  const [statusOptions, setStatusOptions] = useState([]);
  const [priorityOptions, setPriorityOptions] = useState([]);
  const [categoryOptions, setCategoryOptions] = useState([]);
  
  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';

  // Kategori etiketlerini Türkçe'ye çevir
  const getCategoryLabel = (category) => {
    const config = {
      'Complaint': 'Şikayet',
      'Request': 'Talep',
      'Information': 'Bilgi',
      'Technical': 'Teknik Destek'
    };
    return config[category] || category || '-';
  };

  useEffect(() => {
    fetchFilters();
  }, []);

  const fetchTickets = useCallback(async () => {
    try {
      setLoading(true);
      const params = { page, pageSize, ...filters };
      const response = isAdmin 
        ? await ticketApi.getAllIncludingAll(params)
        : await ticketApi.getAll(params);
      setTickets(response.data || []);
      setTotalPages(response.totalPages || 1);
      setTotalCount(response.totalCount || 0);
    } catch (error) {
      toast.error('Ticketlar yüklenemedi');
    } finally {
      setLoading(false);
    }
  }, [page, filters, isAdmin]);

  useEffect(() => {
    fetchTickets();
  }, [page, filters, refreshSignal, fetchTickets]);

  const fetchFilters = async () => {
    try {
      const [statusRes, priorityRes, categoryRes] = await Promise.all([
        ticketApi.getStatusList(),
        ticketApi.getPriorityList(),
        ticketApi.getCategoryList()
      ]);
      setStatusOptions(statusRes);
      setPriorityOptions(priorityRes);
      setCategoryOptions(categoryRes);
    } catch (error) {
      console.error('Filtreler yüklenemedi:', error);
    }
  };

  const handleFilterChange = (key, value) => {
    setFilters(prev => ({ ...prev, [key]: value }));
    setPage(1);
  };

  const handleClearFilters = () => {
    setFilters({ search: '', status: '', priority: '', category: '' });
    setPage(1);
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
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Destek Talepleri</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Toplam {totalCount} talep
            {isConnected && <span className="ml-2 text-green-500 text-xs">🟢 Canlı</span>}
          </p>
        </div>
        <Link to="/tickets/create" className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg transition-colors flex items-center gap-2">
          <span>+</span> Yeni Talep
        </Link>
      </div>

      <div className="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-800 p-4 mb-6">
        <div className="flex flex-wrap gap-3">
          <input type="text" placeholder="Konu, talep no, müşteri ara..." value={filters.search} onChange={(e) => handleFilterChange('search', e.target.value)} className="flex-1 min-w-[200px] px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 text-sm" />
          <select value={filters.status} onChange={(e) => handleFilterChange('status', e.target.value)} className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm">
            <option value="">Tüm Durumlar</option>
            {statusOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
          </select>
          <select value={filters.priority} onChange={(e) => handleFilterChange('priority', e.target.value)} className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm">
            <option value="">Tüm Öncelikler</option>
            {priorityOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
          </select>
          <select value={filters.category} onChange={(e) => handleFilterChange('category', e.target.value)} className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm">
            <option value="">Tüm Kategoriler</option>
            {categoryOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
          </select>
          <button onClick={handleClearFilters} className="px-4 py-2 bg-gray-500 hover:bg-gray-600 text-white rounded-lg transition-colors text-sm font-medium">Temizle</button>
        </div>
      </div>

      <div className="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-800 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-800">
            <thead className="bg-gray-50 dark:bg-gray-800/50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Talep No</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Konu</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Müşteri</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Öncelik</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Kategori</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tarih</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
              {tickets.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">Henüz talep bulunmuyor</td>
                </tr>
              ) : (
                tickets.map((ticket) => (
                  <tr key={ticket.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                    <td className="px-6 py-4 font-mono text-sm font-medium text-indigo-600 dark:text-indigo-400">{ticket.ticketNumber}</td>
                    <td className="px-6 py-4 text-gray-900 dark:text-gray-100 max-w-xs truncate">{ticket.subject}</td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-400 text-sm">{ticket.customerName}</td>
                    <td className="px-6 py-4">{getPriorityBadge(ticket.priority)}</td>
                    <td className="px-6 py-4">{getStatusBadge(ticket.status)}</td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-400 text-sm">{getCategoryLabel(ticket.category)}</td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-400 text-sm">{new Date(ticket.createdAt).toLocaleDateString('tr-TR')}</td>
                    <td className="px-6 py-4 text-center">
                      <Link to={`/tickets/${ticket.id}`} className="inline-flex items-center gap-1 text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 dark:hover:text-indigo-300 transition-colors">
                        <span>👁️</span> Detay
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {totalPages > 1 && (
        <div className="flex justify-between items-center mt-4 pt-2">
          <div className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} kayıt</div>
          <div className="flex gap-2">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="px-3 py-1 border border-gray-200 dark:border-gray-700 rounded-lg disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors text-gray-700 dark:text-gray-300">◀</button>
            <span className="px-3 py-1 text-sm text-gray-700 dark:text-gray-300">Sayfa {page} / {totalPages}</span>
            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="px-3 py-1 border border-gray-200 dark:border-gray-700 rounded-lg disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors text-gray-700 dark:text-gray-300">▶</button>
          </div>
        </div>
      )}
    </div>
  );
}