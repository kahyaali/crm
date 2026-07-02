import { createContext, useState, useContext, useEffect } from 'react';
import api from '../services/api';

const AuthContext = createContext();

export const useAuth = () => useContext(AuthContext);

// Token'ı manuel decode et
const parseJwt = (token) => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
      return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
    return JSON.parse(jsonPayload);
  } catch (error) {
    return null;
  }
};

const MOCK_PERMISSIONS = [
  'dashboard.view', 'user.view', 'user.create', 'product.view'
];

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [permissions, setPermissions] = useState([]);
  const [loading, setLoading] = useState(true);

  const logout = () => {
    localStorage.clear();
    setUser(null);
    setPermissions([]);
    window.location.href = '/login';
  };

  const fetchPermissions = async () => {
    try {
      const response = await api.get('/Users/permissions');
      setPermissions(response.data || []);
    } catch (error) {
      console.warn('Backend yetkileri alınamadı, varsayılanlar kullanılıyor.');
      setPermissions(MOCK_PERMISSIONS);
    }
  };

  useEffect(() => {
    const initAuth = async () => {
      const token = localStorage.getItem('accessToken');
      
      if (token) {
        const decoded = parseJwt(token);
        
        // Token geçerlilik kontrolü (exp süresi dolmuş mu?)
        const isExpired = decoded?.exp ? decoded.exp * 1000 < Date.now() : true;

        if (isExpired) {
          logout();
        } else {
          // Token geçerli, kullanıcı bilgilerini set et
          const userStr = localStorage.getItem('user');
          const userData = userStr ? JSON.parse(userStr) : {};
          
          setUser({
            ...userData,
            personelId: decoded?.PersonelId || decoded?.personelId || decoded?.nameidentifier,
            role: decoded?.role || userData?.role,
            email: decoded?.email || userData?.email
          });
          
          await fetchPermissions();
        }
      }
      setLoading(false);
    };
    initAuth();
  }, []);

  const hasPermission = (permission) => {
    if (!permission) return true;
    if (user?.role === 'SystemAdmin' || user?.role === 'Admin') return true;
    return permissions.includes(permission);
  };

  const login = async (email, password) => {
    const response = await api.post('/Auth/login', { email, password });
    const { accessToken, refreshToken, ...userData } = response.data;
    
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    
    const decoded = parseJwt(accessToken);
    const finalUserData = {
      ...userData,
      personelId: decoded?.PersonelId || decoded?.personelId,
      role: decoded?.role || userData?.role,
      email: decoded?.email || userData?.email
    };
    
    localStorage.setItem('user', JSON.stringify(finalUserData));
    setUser(finalUserData);
    await fetchPermissions();
    
    return response.data;
  };

  return (
    <AuthContext.Provider value={{
      user, permissions, loading, login, logout, hasPermission
    }}>
      {children}
    </AuthContext.Provider>
  );
};