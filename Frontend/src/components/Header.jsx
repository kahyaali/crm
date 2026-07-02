import { useAuth } from '../contexts/AuthContext';
import { useState, useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import NotificationPanel from './NotificationPanel'; 

export default function Header() {
  const { user, logout } = useAuth();
  const [isDark, setIsDark] = useState(false);
  const [showUserMenu, setShowUserMenu] = useState(false);
  const menuRef = useRef(null);

  // Dark mode başlangıç durumunu kontrol et
  useEffect(() => {
    const isDarkMode = document.documentElement.classList.contains('dark');
    setIsDark(isDarkMode);
  }, []);

  // Dışarı tıklanınca menüyü kapat
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (menuRef.current && !menuRef.current.contains(event.target)) {
        setShowUserMenu(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const toggleDarkMode = () => {
    setIsDark(!isDark);
    if (!isDark) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  };

  return (
    <header className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-6 py-4 flex justify-between items-center sticky top-0 z-10">
      <div>
        <h2 className="text-xl font-semibold text-gray-800 dark:text-white">
          Hoşgeldin, {user?.firstName} {user?.lastName}
        </h2>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Rol: {user?.role === 'SystemAdmin' ? 'Sistem Yöneticisi' : 
                user?.role === 'Admin' ? 'Yönetici' : 
                user?.role === 'Manager' ? 'Yönetici' : 'Personel'}
        </p>
      </div>
      
      <div className="flex items-center gap-4">
        {/* BİLDİRİM PANELİ */}
        <NotificationPanel />

        {/* Dark Mode Butonu */}
        <button
          onClick={toggleDarkMode}
          className="p-2 rounded-lg bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          title={isDark ? 'Açık Moda Geç' : 'Karanlık Moda Geç'}
        >
          {isDark ? '☀️' : '🌙'}
        </button>

        {/* Kullanıcı Menüsü */}
        <div className="relative" ref={menuRef}>
          <button
            onClick={() => setShowUserMenu(!showUserMenu)}
            className="flex items-center gap-2 p-1 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
          >
            <div className="w-9 h-9 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-semibold">
              {user?.firstName?.charAt(0)}{user?.lastName?.charAt(0)}
            </div>
            <span className="hidden md:inline text-gray-700 dark:text-gray-300">
              {user?.firstName} {user?.lastName}
            </span>
            <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          {/* Dropdown Menü */}
          {showUserMenu && (
            <div className="absolute right-0 mt-2 w-56 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 z-50 overflow-hidden">
              <div className="px-4 py-3 border-b border-gray-200 dark:border-gray-700">
                <p className="text-sm font-medium text-gray-900 dark:text-white">
                  {user?.firstName} {user?.lastName}
                </p>
                <p className="text-xs text-gray-500 dark:text-gray-400 truncate">
                  {user?.email}
                </p>
              </div>
              
              <Link
                to="/profile"
                onClick={() => setShowUserMenu(false)}
                className="flex items-center gap-3 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              >
                <span>👤</span> Profilim
              </Link>
              
              <Link
                to="/dashboard"
                onClick={() => setShowUserMenu(false)}
                className="flex items-center gap-3 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              >
                <span>📊</span> Dashboard
              </Link>
              
              <hr className="my-1 border-gray-200 dark:border-gray-700" />
              
              <button
                onClick={() => {
                  setShowUserMenu(false);
                  logout();
                }}
                className="flex items-center gap-3 w-full text-left px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 transition-colors"
              >
                <span>🚪</span> Çıkış Yap
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}