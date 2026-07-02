import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function AdminRoute({ children }) {
  const { user, loading } = useAuth();
  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
  
  if (loading) return <div>Yükleniyor...</div>;
  return isAdmin ? children : <Navigate to="/dashboard" />;
}