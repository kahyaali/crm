import { NavLink } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useSignalR } from '../contexts/SignalRContext';
import CompanyLogo from './CompanyLogo';

export default function Sidebar() {
  const { user } = useAuth();
  const { isConnected } = useSignalR();
  
  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
  const isManager = user?.role === 'Manager' || isAdmin;
  const isPersonel = user?.role === 'Personel';

  // Menü öğeleri
  const allMenuItems = [
    // Ana Sayfa
    { path: '/dashboard', icon: '📊', name: 'Dashboard', roles: ['all'] },
    { path: '/profile', icon: '👤', name: 'Profilim', roles: ['all'] },
    
    // 📈 RAPORLAR 
    { path: '/reports', icon: '📈', name: 'Raporlar', roles: ['all'] },
    
    // Müşteri Yönetimi
    { path: '/customers', icon: '👥', name: 'Müşteriler', roles: ['all'] },
    { path: '/my-customers', icon: '📋', name: 'Müşterilerim', roles: ['all'] },
    { path: '/leads', icon: '🎯', name: 'Potansiyel Müşteriler', roles: ['all'] },
    { path: '/opportunities', icon: '💼', name: 'Fırsatlar', roles: ['all'] },
    
    // Satış & Finans
    { path: '/orders', icon: '🛒', name: 'Siparişler', roles: ['all'] },
    { path: '/invoices', icon: '📄', name: 'Faturalar', roles: ['all'] },
    { path: '/quotes', icon: '💰', name: 'Teklifler', roles: ['all'] },
    { path: '/contracts', icon: '📑', name: 'Sözleşmeler', roles: ['all'] },
    
    // Ürün Yönetimi
    { path: '/products', icon: '📦', name: 'Ürünler', roles: ['all'] },
    { path: '/product-categories', icon: '🏷️', name: 'Ürün Kategorileri', roles: ['all'] },
    { path: '/brands', icon: '🏷️', name: 'Markalar', roles: ['admin', 'manager'] },
    
    // Destek & Görevler
    { path: '/tickets', icon: '🎫', name: 'Destek Talepleri', roles: ['all'], badge: isConnected ? '🟢' : '🔴' },
    { path: '/meetings', icon: '📅', name: 'Toplantılar', roles: ['all'] },
    { path: '/tasks', icon: '✅', name: 'Görevler', roles: ['all'] },
    
    // Kampanya
    { path: '/campaigns', icon: '📢', name: 'Kampanyalar', roles: ['all'] },
    
    // Personel Yönetimi
    { path: '/personels', icon: '👥', name: 'Personeller', roles: ['all'] },
    { path: '/my-team', icon: '👥', name: 'Takımım', roles: ['all'] },
    { path: '/departments', icon: '🏢', name: 'Departmanlar', roles: ['all'] },
    { path: '/positions', icon: '💼', name: 'Pozisyonlar', roles: ['all'] },
    
    // Sistem Yönetimi
    { path: '/users', icon: '👑', name: 'Kullanıcılar', roles: ['all'] },
    { path: '/roles', icon: '🔐', name: 'Roller ve Yetkiler', roles: ['all'] },
    { path: '/mail-settings', icon: '📧', name: 'Mail Ayarları', roles: ['all'] },
    { path: '/exchange-rate-settings', icon: '💱', name: 'Kur Servisi', roles: ['all'] },
    { path: '/notifications', icon: '🔔', name: 'Bildirimler', roles: ['all'] },
    
    // Loglar
    { path: '/logs/actions', icon: '📋', name: 'Aksiyon Logları', roles: ['all'] },
    { path: '/logs/errors', icon: '⚠️', name: 'Hata Logları', roles: ['all'] },
  ];

  // Rol kontrol fonksiyonu
  const hasRole = (itemRoles) => {
    if (itemRoles.includes('all')) return true;
    if (itemRoles.includes('admin') && isAdmin) return true;
    if (itemRoles.includes('manager') && isManager) return true;
    if (itemRoles.includes('personel') && isPersonel) return true;
    return false;
  };

  // Menüyü filtrele
  const menuItems = allMenuItems.filter(item => hasRole(item.roles));

  // Menüyü gruplara ayır
  const groups = [
    {
      name: 'Ana Sayfa',
      icon: '🏠',
      items: ['/dashboard', '/profile']
    },
    {
      name: '📊 Raporlar',
      icon: '📊',
      items: ['/reports']
    },
    {
      name: 'Müşteri Yönetimi',
      icon: '👥',
      items: ['/customers', '/my-customers', '/leads', '/opportunities']
    },
    {
      name: 'Satış & Finans',
      icon: '💰',
      items: ['/orders', '/invoices', '/quotes', '/contracts']
    },
    {
      name: 'Ürün Yönetimi',
      icon: '📦',
      items: ['/products', '/product-categories', '/brands']
    },
    {
      name: 'Destek & Görevler',
      icon: '🎫',
      items: ['/tickets', '/meetings', '/tasks']
    },
    {
      name: 'Kampanya',
      icon: '📢',
      items: ['/campaigns']
    },
    {
      name: 'Personel Yönetimi',
      icon: '👥',
      items: ['/personels', '/my-team', '/departments', '/positions']
    },
    {
      name: 'Sistem Yönetimi',
      icon: '⚙️',
      items: ['/users', '/roles', '/mail-settings', '/exchange-rate-settings', '/notifications']
    },
    {
      name: 'Loglar',
      icon: '📋',
      items: ['/logs/actions', '/logs/errors']
    }
  ];

  // Grup içindeki öğelerin görünürlüğünü kontrol et
  const getGroupItems = (groupPaths) => {
    return groupPaths
      .map(path => menuItems.find(item => item.path === path))
      .filter(Boolean);
  };

  // Görünür grupları filtrele
  const visibleGroups = groups
    .map(group => ({
      ...group,
      items: getGroupItems(group.items)
    }))
    .filter(group => group.items.length > 0);

  return (
    <aside className="w-64 bg-gray-900 text-white flex-shrink-0 h-screen overflow-hidden flex flex-col">
      {/* Logo ve Başlık */}
      <div className="p-5 border-b border-gray-700 flex-shrink-0">
        <div className="flex items-center gap-3 mb-4">
          <CompanyLogo />
          <div>
            <h2 className="font-semibold text-white text-sm">CRM Panel</h2>
            <p className="text-xs text-gray-400">Yönetim Paneli</p>
          </div>
        </div>
        
        {/* Kullanıcı Bilgisi */}
        <div className="bg-gray-800 rounded-xl p-3">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center text-sm font-semibold flex-shrink-0">
              {user?.firstName?.charAt(0)}{user?.lastName?.charAt(0)}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-white truncate">
                {user?.firstName} {user?.lastName}
              </p>
              <p className="text-xs text-gray-400 truncate">
                {user?.role === 'SystemAdmin' ? 'Sistem Admin' : 
                 user?.role === 'Admin' ? 'Admin' : 
                 user?.role === 'Manager' ? 'Yönetici' : 'Personel'}
              </p>
            </div>
          </div>
        </div>
      </div>
      
      {/* Menü */}
      <nav className="flex-1 overflow-y-auto p-4">
        {visibleGroups.map((group, groupIndex) => (
          <div key={groupIndex} className="mb-4">
            {/* Grup Başlığı */}
            <div className="flex items-center gap-2 px-3 py-1.5 mb-1">
              <span className="text-sm">{group.icon}</span>
              <span className="text-xs font-semibold text-gray-400 uppercase tracking-wider">
                {group.name}
              </span>
            </div>
            
            {/* Grup Öğeleri */}
            <div className="space-y-0.5">
              {group.items.map((item) => (
                <NavLink
                  key={item.path}
                  to={item.path}
                  className={({ isActive }) =>
                    `flex items-center gap-3 px-4 py-2.5 rounded-xl transition-all duration-200 ${
                      isActive
                        ? 'bg-blue-600 text-white'
                        : 'text-gray-400 hover:bg-gray-800 hover:text-white'
                    }`
                  }
                >
                  <span className="text-lg">{item.icon}</span>
                  <span className="text-sm flex-1">{item.name}</span>
                  {item.badge && (
                    <span className="text-xs">{item.badge}</span>
                  )}
                </NavLink>
              ))}
            </div>
          </div>
        ))}

        {/* Alt Bilgi */}
        <div className="mt-6 pt-4 border-t border-gray-700 text-center">
          <p className="text-xs text-gray-500">
            v1.0.0 • {isConnected ? '🟢 Bağlı' : '🔴 Bağlantı Yok'}
          </p>
        </div>
      </nav>
    </aside>
  );
}