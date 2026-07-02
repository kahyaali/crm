// src/hooks/usePermissions.js
import { useAuth } from '../contexts/AuthContext';
import { DEFAULT_ROLE_PERMISSIONS } from '../constants/permissions';

export const usePermissions = () => {
  const { user, permissions: userPermissions } = useAuth();

  const hasPermission = (permission) => {
    if (!permission) return true;
    
    // SystemAdmin her şeyi yapabilir
    if (user?.role === 'SystemAdmin') return true;
    
    // Admin her şeyi yapabilir
    if (user?.role === 'Admin') return true;
    
    // Backend'den gelen yetkiler varsa onları kullan
    if (userPermissions && userPermissions.length > 0) {
      return userPermissions.includes(permission);
    }
    
    // Backend'den gelmediyse varsayılan rolleri kullan
    const defaultPermissions = DEFAULT_ROLE_PERMISSIONS[user?.role] || [];
    return defaultPermissions.includes(permission);
  };

  const hasAnyPermission = (permissionList) => {
    if (!permissionList || permissionList.length === 0) return true;
    return permissionList.some(p => hasPermission(p));
  };

  const hasAllPermissions = (permissionList) => {
    if (!permissionList || permissionList.length === 0) return true;
    return permissionList.every(p => hasPermission(p));
  };

  const getRoleName = () => {
    const names = {
      SystemAdmin: 'Sistem Admin',
      Admin: 'Admin',
      Manager: 'Yönetici',
      Personel: 'Personel',
      Viewer: 'Görüntüleyici'
    };
    return names[user?.role] || user?.role || 'Kullanıcı';
  };

  const isAdmin = () => {
    return user?.role === 'SystemAdmin' || user?.role === 'Admin';
  };

  const isManager = () => {
    return user?.role === 'Manager' || isAdmin();
  };

  const isPersonel = () => {
    return user?.role === 'Personel';
  };

  const isViewer = () => {
    return user?.role === 'Viewer';
  };

  return {
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    getRoleName,
    isAdmin,
    isManager,
    isPersonel,
    isViewer,
  };
};