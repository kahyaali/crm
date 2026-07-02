// src/pages/Dashboard.jsx
import { useAuth } from '../contexts/AuthContext';
import { usePermissions } from '../hooks/usePermissions';
import { PERMISSIONS } from '../constants/permissions';
import { useState, useEffect, useCallback, useRef } from 'react';
import api from '../services/api';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  FaUsers, FaUserPlus, FaChartLine, FaTicketAlt, 
  FaShoppingCart, FaMoneyBillWave, FaFileExcel, FaFilePdf,
  FaSync, FaCalendarAlt, FaChevronRight, FaStar,
  FaClock, FaArrowUp, FaArrowDown, FaEye, FaEyeSlash,
  FaBell
} from 'react-icons/fa';
import { toast } from 'react-hot-toast';

export default function Dashboard() {
  const { user } = useAuth();
  const { hasPermission, getRoleName, isAdmin, isPersonel } = usePermissions();
  
  const [stats, setStats] = useState({
    totalCustomers: 0,
    totalLeads: 0,
    totalOrders: 0,
    totalTickets: 0,
    openTickets: 0,
    totalRevenue: 0,
    conversionRate: 0,
    satisfactionRate: 0,
    totalPersonnel: 0,
    closedTickets: 0,
    recentActivities: [],
    charts: []
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [timeFilter, setTimeFilter] = useState('month');
  const [showCharts, setShowCharts] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  // Refs
  const abortControllerRef = useRef(null);
  const isFetchingRef = useRef(false);
  const isMountedRef = useRef(true);

  // Yetki kontrolleri
  const canViewCustomers = hasPermission(PERMISSIONS.VIEW_CUSTOMERS);
  const canViewLeads = hasPermission(PERMISSIONS.VIEW_LEADS);
  const canViewTickets = hasPermission(PERMISSIONS.VIEW_TICKETS);
  const canViewOrders = hasPermission(PERMISSIONS.VIEW_ORDERS);
  const canViewReports = hasPermission(PERMISSIONS.VIEW_REPORTS);
  const canExport = hasPermission(PERMISSIONS.EXPORT_REPORTS);
  const canCreateCustomer = hasPermission(PERMISSIONS.CREATE_CUSTOMER);
  const canCreateLead = hasPermission(PERMISSIONS.CREATE_LEAD);
  const canCreateTicket = hasPermission(PERMISSIONS.CREATE_TICKET);
  const canCreateOrder = hasPermission(PERMISSIONS.CREATE_ORDER);

  const fetchStats = useCallback(async (showRefresh = false) => {
    if (isFetchingRef.current) {
      console.log('⏳ Zaten fetch yapılıyor...');
      return;
    }

    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    const controller = new AbortController();
    abortControllerRef.current = controller;

    try {
      isFetchingRef.current = true;
      if (showRefresh) {
        setRefreshing(true);
      } else {
        setLoading(true);
      }
      setError(null);
      
      const endpoint = isAdmin() ? '/Dashboard/stats' : '/Dashboard/personel-stats';
      
      console.log('📤 Dashboard isteği:', endpoint);
      
      const response = await api.get(endpoint, {
        params: { 
          timeFilter,
          userId: !isAdmin() ? user?.id : undefined
        },
        signal: controller.signal,
      });
      
      console.log('📥 Dashboard yanıtı:', response.data);
      
      if (isMountedRef.current && response.data?.data) {
        const data = response.data.data;
        setStats({
          totalCustomers: data.kpi?.totalCustomers || 0,
          totalLeads: data.kpi?.totalLeads || 0,
          totalOrders: data.kpi?.totalOrders || 0,
          totalTickets: data.kpi?.totalTickets || 0,
          openTickets: data.kpi?.openTickets || 0,
          totalRevenue: data.kpi?.totalRevenue || 0,
          conversionRate: data.kpi?.conversionRate || 0,
          satisfactionRate: data.kpi?.satisfactionRate || 0,
          totalPersonnel: data.kpi?.totalPersonnel || 0,
          closedTickets: data.kpi?.closedTickets || 0,
          recentActivities: data.recentActivities || [],
          charts: data.charts || []
        });
        console.log('✅ Stats set edildi:', stats);
      }
      
      if (showRefresh) {
        toast.success('Dashboard yenilendi!');
      }
    } catch (error) {
      if (error.name === 'AbortError' || error.code === 'ERR_CANCELED') {
        console.log('⏹️ İstek iptal edildi');
        return;
      }
      
      console.error('Dashboard yüklenemedi:', error);
      
      if (isMountedRef.current) {
        if (error.response?.status === 429) {
          setError('Çok fazla istek gönderildi. Lütfen 30 saniye bekleyin.');
          toast.error('Çok fazla istek! Lütfen bekleyin.');
          setTimeout(() => {
            if (!isFetchingRef.current && isMountedRef.current) {
              fetchStats();
            }
          }, 30000);
        } else if (error.response?.status === 401) {
          setError('Oturum süresi doldu. Lütfen tekrar giriş yapın.');
          toast.error('Oturum süresi doldu!');
        } else if (error.code === 'ECONNABORTED' || error.message?.includes('timeout')) {
          setError('Sunucu yanıt vermiyor. Lütfen daha sonra tekrar deneyin.');
          toast.error('İstek zaman aşımına uğradı!');
        } else {
          setError('Dashboard verileri yüklenirken bir hata oluştu.');
          toast.error('Dashboard yüklenemedi!');
        }
      }
    } finally {
      if (isMountedRef.current) {
        isFetchingRef.current = false;
        if (showRefresh) setRefreshing(false);
        else setLoading(false);
      }
      abortControllerRef.current = null;
    }
  }, [timeFilter, user, isAdmin]);

  useEffect(() => {
    isMountedRef.current = true;
    
    const timer = setTimeout(() => {
      fetchStats();
    }, 200);
    
    return () => {
      isMountedRef.current = false;
      clearTimeout(timer);
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
      isFetchingRef.current = false;
    };
  }, []);

  const handleTimeFilterChange = (newFilter) => {
    if (newFilter !== timeFilter) {
      setTimeFilter(newFilter);
      setTimeout(() => {
        if (!isFetchingRef.current && isMountedRef.current) {
          fetchStats();
        }
      }, 100);
    }
  };

  const handleRefresh = () => {
    if (!isFetchingRef.current && isMountedRef.current) {
      fetchStats(true);
    }
  };

const handleExport = async (type) => {
  try {
    let startDate = null;
    let endDate = null;
    const now = new Date();
    
    switch (timeFilter) {
      case 'today':
        startDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        endDate = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
        break;
      case 'week':
        const day = now.getDay();
        const diff = now.getDate() - day + (day === 0 ? -6 : 1);
        startDate = new Date(now.getFullYear(), now.getMonth(), diff);
        endDate = new Date(startDate);
        endDate.setDate(endDate.getDate() + 7);
        break;
      case 'month':
        startDate = new Date(now.getFullYear(), now.getMonth(), 1);
        endDate = new Date(now.getFullYear(), now.getMonth() + 1, 1);
        break;
      case 'year':
        startDate = new Date(now.getFullYear(), 0, 1);
        endDate = new Date(now.getFullYear() + 1, 0, 1);
        break;
      default:
        startDate = new Date(now.getFullYear(), now.getMonth() - 1, 1);
        endDate = new Date(now.getFullYear(), now.getMonth() + 1, 1);
    }

    // 🔥 Date nesnelerini doğrudan gönder (JSON.stringify otomatik çevirir)
    const exportData = {
      timeFilter: timeFilter,
      userId: !isAdmin() ? user?.id : undefined,
      startDate: startDate,
      endDate: endDate
    };

    console.log('📤 Export isteği:', exportData);

    const response = await api.post(`/Dashboard/export/${type}`, exportData, { 
      responseType: 'blob',
      timeout: 120000,
    });
    
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `Dashboard_${new Date().toISOString().slice(0,10)}.${type === 'excel' ? 'xlsx' : 'pdf'}`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
    
    toast.success(`${type.toUpperCase()} başarıyla indirildi!`);
  } catch (error) {
    console.error('Export hatası:', error);
    toast.error(`${type.toUpperCase()} indirilemedi!`);
  }
};


  if (loading) {
    return (
      <div className="flex justify-center items-center h-[80vh]">
        <div className="relative">
          <div className="animate-spin rounded-full h-20 w-20 border-4 border-indigo-500/20 border-t-indigo-600"></div>
          <div className="absolute inset-0 flex items-center justify-center">
            <span className="text-xs font-medium text-indigo-600 animate-pulse">YÜKLENİYOR</span>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-[80vh] p-6">
        <div className="text-6xl mb-4">⚠️</div>
        <h3 className="text-xl font-semibold text-gray-700 dark:text-gray-300">Bir Hata Oluştu</h3>
        <p className="text-gray-500 dark:text-gray-400 mt-2 text-center max-w-md">{error}</p>
        <button
          onClick={handleRefresh}
          disabled={isFetchingRef.current}
          className="mt-4 px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition disabled:opacity-50 flex items-center gap-2"
        >
          <FaSync className={isFetchingRef.current ? 'animate-spin' : ''} />
          {isFetchingRef.current ? 'Yenileniyor...' : 'Tekrar Dene'}
        </button>
      </div>
    );
  }

  const roleName = getRoleName();
  const greeting = isAdmin() ? 'Şirket Geneli' : isPersonel() ? 'Kişisel' : 'Genel';

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 p-4 md:p-6">
      <div className="max-w-7xl mx-auto">
      {/* HEADER */}
<div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
  <div>
    <h1 className="text-2xl md:text-3xl font-bold text-gray-900 dark:text-white">
      Merhaba, {user?.firstName || 'Ziyaretçi'}! 👋
    </h1>
    <div className="flex items-center gap-3 mt-1 flex-wrap">
      <span className="text-sm text-gray-500 dark:text-gray-400">
        {new Date().toLocaleDateString('tr-TR', { 
          weekday: 'long', 
          year: 'numeric', 
          month: 'long', 
          day: 'numeric' 
        })}
      </span>
      <span className="px-3 py-1 bg-indigo-100 dark:bg-indigo-900/50 text-indigo-700 dark:text-indigo-300 rounded-full text-xs font-medium">
        {roleName}
      </span>
      <span className="px-3 py-1 bg-emerald-100 dark:bg-emerald-900/50 text-emerald-700 dark:text-emerald-300 rounded-full text-xs font-medium">
        {greeting} Görünüm
      </span>
    </div>
  </div>
  
  <div className="flex items-center gap-2 flex-wrap">
    {/* Zaman Filtresi */}
    <div className="relative">
      <select 
        value={timeFilter}
        onChange={(e) => handleTimeFilterChange(e.target.value)}
        className="appearance-none pl-10 pr-4 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-300 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent cursor-pointer"
      >
        <option value="today">📅 Bugün</option>
        <option value="week">📅 Bu Hafta</option>
        <option value="month">📅 Bu Ay</option>
        <option value="year">📅 Bu Yıl</option>
      </select>
      <FaCalendarAlt className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm" />
    </div>
    
    {/* Yenile Butonu */}
    <button 
      onClick={handleRefresh}
      disabled={isFetchingRef.current}
      className="px-4 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl hover:bg-gray-50 dark:hover:bg-gray-700 transition-all flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300 disabled:opacity-50"
    >
      <FaSync className={isFetchingRef.current ? 'animate-spin' : ''} />
      {isFetchingRef.current ? 'Yenileniyor...' : 'Yenile'}
    </button>
  </div>
</div>
       

        {/* KPI KARTLARI */}
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 mb-6">
          {canViewCustomers && (
            <KpiCard
              icon={<FaUsers className="text-xl" />}
              title="Toplam Müşteri"
              value={stats.totalCustomers}
              color="blue"
              delay={0}
            />
          )}
          {canViewLeads && (
            <KpiCard
              icon={<FaUserPlus className="text-xl" />}
              title="Potansiyel Müşteri"
              value={stats.totalLeads}
              color="purple"
              delay={1}
            />
          )}
          {canViewTickets && (
            <KpiCard
              icon={<FaTicketAlt className="text-xl" />}
              title="Destek Talebi"
              value={stats.totalTickets}
              subtitle={`Açık: ${stats.openTickets}`}
              color="orange"
              delay={2}
            />
          )}
          {canViewOrders && (
            <KpiCard
              icon={<FaShoppingCart className="text-xl" />}
              title="Toplam Sipariş"
              value={stats.totalOrders}
              color="emerald"
              delay={3}
            />
          )}
          {canViewReports && (
            <KpiCard
              icon={<FaMoneyBillWave className="text-xl" />}
              title="Aylık Gelir"
              value={`₺${stats.totalRevenue.toLocaleString('tr-TR')}`}
              color="green"
              delay={4}
            />
          )}
          {canViewReports && (
            <KpiCard
              icon={<FaChartLine className="text-xl" />}
              title="Dönüşüm Oranı"
              value={`${stats.conversionRate}%`}
              color="indigo"
              delay={5}
            />
          )}
          {isAdmin() && (
            <KpiCard
              icon={<FaStar className="text-xl" />}
              title="Müşteri Memnuniyeti"
              value={`${stats.satisfactionRate}%`}
              color="yellow"
              delay={6}
            />
          )}
          {canViewTickets && (
            <KpiCard
              icon={<FaClock className="text-xl" />}
              title="Açık Talepler"
              value={stats.openTickets}
              subtitle={`Kapandı: ${stats.closedTickets}`}
              color="red"
              delay={7}
            />
          )}
        </div>

        {/* GRAFİKLER */}
        {canViewReports && stats.charts?.length > 0 && (
          <div className="mb-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white flex items-center gap-2">
                <FaChartLine className="text-indigo-600" />
                Analizler ve Grafikler
              </h2>
              <button 
                onClick={() => setShowCharts(!showCharts)}
                className="text-sm text-indigo-600 dark:text-indigo-400 hover:underline flex items-center gap-1"
              >
                {showCharts ? <FaEyeSlash /> : <FaEye />}
                {showCharts ? 'Gizle' : 'Göster'}
              </button>
            </div>
            
            <AnimatePresence>
              {showCharts && (
                <motion.div 
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -20 }}
                  className="grid grid-cols-1 lg:grid-cols-2 gap-6"
                >
                  {stats.charts.map((chart, index) => (
                    <ChartCard key={index} chart={chart} />
                  ))}
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        )}

        {/* SON AKTİVİTELER */}
        <div className="grid grid-cols-1 gap-6 mb-6">
          <ActivityList 
            activities={stats.recentActivities || []} 
            isAdmin={isAdmin()}
            userId={user?.id}
          />
        </div>

        {/* BİLDİRİMLER */}
        <NotificationBar />
      </div>
    </div>
  );
}

// ============================================================
//  ALT COMPONENT'LER
// ============================================================

function KpiCard({ icon, title, value, subtitle, color, delay = 0 }) {
  const colors = {
    blue: { bg: 'from-blue-500/20 to-blue-600/20', iconBg: 'from-blue-500 to-blue-600', border: 'border-blue-200 dark:border-blue-900/30' },
    purple: { bg: 'from-purple-500/20 to-purple-600/20', iconBg: 'from-purple-500 to-purple-600', border: 'border-purple-200 dark:border-purple-900/30' },
    orange: { bg: 'from-orange-500/20 to-orange-600/20', iconBg: 'from-orange-500 to-orange-600', border: 'border-orange-200 dark:border-orange-900/30' },
    emerald: { bg: 'from-emerald-500/20 to-emerald-600/20', iconBg: 'from-emerald-500 to-emerald-600', border: 'border-emerald-200 dark:border-emerald-900/30' },
    green: { bg: 'from-green-500/20 to-green-600/20', iconBg: 'from-green-500 to-green-600', border: 'border-green-200 dark:border-green-900/30' },
    indigo: { bg: 'from-indigo-500/20 to-indigo-600/20', iconBg: 'from-indigo-500 to-indigo-600', border: 'border-indigo-200 dark:border-indigo-900/30' },
    yellow: { bg: 'from-yellow-500/20 to-yellow-600/20', iconBg: 'from-yellow-500 to-yellow-600', border: 'border-yellow-200 dark:border-yellow-900/30' },
    red: { bg: 'from-red-500/20 to-red-600/20', iconBg: 'from-red-500 to-red-600', border: 'border-red-200 dark:border-red-900/30' },
  };

  const c = colors[color] || colors.blue;

  return (
    <motion.div
      initial={{ scale: 0.9, opacity: 0 }}
      animate={{ scale: 1, opacity: 1 }}
      transition={{ delay: delay * 0.05, duration: 0.3 }}
      className={`bg-gradient-to-br ${c.bg} bg-white dark:bg-gray-800/50 backdrop-blur-sm rounded-xl border ${c.border} p-5 shadow-sm hover:shadow-lg transition-all duration-300 hover:-translate-y-1`}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-gray-500 dark:text-gray-400 truncate">{title}</p>
          <p className="text-2xl font-bold text-gray-900 dark:text-white mt-1 truncate">{value}</p>
          {subtitle && <p className="text-xs text-gray-400 dark:text-gray-500 mt-0.5">{subtitle}</p>}
        </div>
        <div className={`w-12 h-12 bg-gradient-to-br ${c.iconBg} rounded-xl flex items-center justify-center text-white shadow-lg flex-shrink-0`}>
          {icon}
        </div>
      </div>
    </motion.div>
  );
}

function ChartCard({ chart }) {
  const colors = ['#4F46E5', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'];

  return (
    <div className="bg-white dark:bg-gray-800/50 backdrop-blur-sm rounded-xl border border-gray-200 dark:border-gray-700/50 p-5 shadow-sm hover:shadow-md transition-all">
      <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">{chart.title}</h3>
      <div className="h-64 flex items-center justify-center">
        <div className="w-full h-full bg-gray-50 dark:bg-gray-800/30 rounded-lg flex flex-col items-center justify-center">
          <div className="text-4xl mb-2">📊</div>
          <p className="text-sm text-gray-400 dark:text-gray-500">{chart.type} Grafiği</p>
          <p className="text-xs text-gray-300 dark:text-gray-600 mt-1">{chart.data?.length || 0} veri noktası</p>
          <div className="flex items-end gap-1 mt-4 h-16">
            {(chart.data || []).slice(0, 12).map((item, i) => (
              <div 
                key={i}
                className="w-4 rounded-t transition-all hover:opacity-80"
                style={{
                  height: `${Math.max(10, (item.value / Math.max(1, ...(chart.data || []).map(d => d.value))) * 60)}px`,
                  backgroundColor: colors[i % colors.length]
                }}
                title={`${item.label}: ${item.value}`}
              />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function ActivityList({ activities, isAdmin, userId }) {
  const filtered = isAdmin ? activities : activities.filter(a => a.userId === userId || a.assignedToUserId === userId);
  const displayActivities = filtered.slice(0, 8);

  const icons = {
    Ticket: '🎫', Lead: '🎯', Customer: '👤', Order: '🛒',
    Invoice: '📄', Contract: '📝', Meeting: '📅', Task: '✅',
    Opportunity: '💼', Campaign: '📢',
  };

  const colors = {
    Ticket: 'bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400',
    Lead: 'bg-purple-100 dark:bg-purple-900/30 text-purple-600 dark:text-purple-400',
    Customer: 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-600 dark:text-emerald-400',
    Order: 'bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400',
    Invoice: 'bg-green-100 dark:bg-green-900/30 text-green-600 dark:text-green-400',
    Contract: 'bg-pink-100 dark:bg-pink-900/30 text-pink-600 dark:text-pink-400',
    Meeting: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-600 dark:text-yellow-400',
    Task: 'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400',
    Opportunity: 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-600 dark:text-indigo-400',
    Campaign: 'bg-cyan-100 dark:bg-cyan-900/30 text-cyan-600 dark:text-cyan-400',
  };

  return (
    <div className="bg-white dark:bg-gray-800/50 backdrop-blur-sm rounded-xl border border-gray-200 dark:border-gray-700/50 p-5 shadow-sm">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 flex items-center gap-2">
          <FaBell className="text-indigo-500" />
          {isAdmin ? 'Son Aktiviteler (Tüm Şirket)' : 'Kişisel Aktivitelerim'}
        </h3>
        {activities.length > 8 && (
          <button className="text-xs text-indigo-600 dark:text-indigo-400 hover:underline flex items-center gap-1">
            Tümünü Gör <FaChevronRight className="text-xs" />
          </button>
        )}
      </div>
      
      <div className="space-y-3 max-h-[400px] overflow-y-auto pr-1">
        {displayActivities.length > 0 ? (
          displayActivities.map((activity, index) => (
            <motion.div
              key={index}
              initial={{ opacity: 0, x: -10 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: index * 0.05 }}
              className="flex items-center gap-3 p-3 bg-gray-50 dark:bg-gray-700/20 rounded-xl hover:bg-gray-100 dark:hover:bg-gray-700/30 transition-all group"
            >
              <div className={`w-10 h-10 rounded-full ${colors[activity.entityType] || 'bg-gray-100 dark:bg-gray-700'} flex items-center justify-center text-lg flex-shrink-0`}>
                {icons[activity.entityType] || '📌'}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-800 dark:text-white truncate">{activity.title}</p>
                <p className="text-xs text-gray-500 dark:text-gray-400 truncate">
                  {activity.action} · {activity.userName}
                </p>
              </div>
              <span className="text-xs text-gray-400 dark:text-gray-500 whitespace-nowrap flex-shrink-0 group-hover:text-gray-600 dark:group-hover:text-gray-300 transition">
                {activity.timeAgo}
              </span>
            </motion.div>
          ))
        ) : (
          <div className="text-center py-8">
            <div className="text-4xl mb-2">📭</div>
            <p className="text-sm text-gray-400 dark:text-gray-500">Henüz aktivite yok</p>
          </div>
        )}
      </div>
    </div>
  );
}

function NotificationBar() {
  const notifications = [
    { type: 'info', message: 'Sistem bakımı 25 Haziran 2024 tarihinde yapılacaktır.' },
    { type: 'success', message: 'Yeni müşteri kaydı sistemi aktif!', link: '#' },
    { type: 'warning', message: '3 adet atanmamış ticket bulunuyor.' },
  ];

  if (notifications.length === 0) return null;

  return (
    <div className="bg-gradient-to-r from-indigo-50 to-purple-50 dark:from-indigo-950/30 dark:to-purple-950/30 border border-indigo-200 dark:border-indigo-800/30 rounded-xl p-4 flex items-center justify-between flex-wrap gap-3">
      <div className="flex items-center gap-3 flex-1 min-w-0">
        <span className="text-2xl">📢</span>
        <div className="flex-1 min-w-0">
          <p className="text-sm text-gray-700 dark:text-gray-300 truncate">
            {notifications[0].message}
          </p>
        </div>
      </div>
      <div className="flex items-center gap-2 flex-shrink-0">
        {notifications.slice(1).map((n, i) => (
          <span key={i} className="w-2 h-2 rounded-full bg-indigo-400 animate-pulse" />
        ))}
        <button className="text-xs text-indigo-600 dark:text-indigo-400 hover:underline whitespace-nowrap">
          Tüm Bildirimler →
        </button>
      </div>
    </div>
  );
}