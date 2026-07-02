import { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';

export default function Users() {
  const { user } = useAuth();
  const [users, setUsers] = useState([]);
  const [roles, setRoles] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedRoles, setSelectedRoles] = useState({});
  
  // Şifre değiştirme modal state'leri
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [selectedUser, setSelectedUser] = useState(null);
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [changingPassword, setChangingPassword] = useState(false);

  useEffect(() => {
    fetchUsers();
    fetchRoles();
  }, []);

  const fetchUsers = async () => {
    try {
      const response = await api.get('/Users');
      setUsers(response.data);
      const initialRoles = {};
      response.data.forEach(user => {
        initialRoles[user.id] = user.role || 'User';
      });
      setSelectedRoles(initialRoles);
    } catch (error) {
      toast.error('Kullanıcılar yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const fetchRoles = async () => {
    try {
      const response = await api.get('/Roles');
      setRoles(response.data);
    } catch (error) {
      console.error('Roller yüklenemedi:', error);
    }
  };

  const handleRoleChange = (userId, roleName) => {
    setSelectedRoles(prev => ({ ...prev, [userId]: roleName }));
  };

  const handleSaveRole = async (userId, currentRole) => {
    const newRole = selectedRoles[userId];
    
    if (newRole === currentRole) {
      toast.error('Aynı rolü seçtiniz, lütfen farklı bir rol seçin');
      return;
    }

    try {
      await api.put(`/Users/${userId}/role`, { role: newRole });
      toast.success('Rol başarıyla güncellendi');
      fetchUsers();
    } catch (error) {
      toast.error(error.response?.data?.message || 'Rol değiştirilemedi');
    }
  };

  // Şifre değiştirme fonksiyonu
  const handleOpenPasswordModal = (userItem) => {
    setSelectedUser(userItem);
    setNewPassword('');
    setConfirmPassword('');
    setShowPasswordModal(true);
  };

  const handleChangePassword = async () => {
    // Validasyonlar
    if (!newPassword || newPassword.length < 6) {
      toast.error('Şifre en az 6 karakter olmalıdır');
      return;
    }
    
    if (newPassword !== confirmPassword) {
      toast.error('Şifreler eşleşmiyor');
      return;
    }
    
    setChangingPassword(true);
    
    try {
      await api.post('/Auth/change-password', {
        userId: selectedUser.id,
        newPassword: newPassword
      });
      
      toast.success(`${selectedUser.firstName} ${selectedUser.lastName} kullanıcısının şifresi başarıyla değiştirildi`);
      setShowPasswordModal(false);
      setNewPassword('');
      setConfirmPassword('');
      setSelectedUser(null);
    } catch (error) {
      toast.error(error.response?.data?.message || 'Şifre değiştirilemedi');
    } finally {
      setChangingPassword(false);
    }
  };

  // Rol badge rengi
  const getRoleBadgeColor = (role) => {
    switch(role) {
      case 'SystemAdmin':
        return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200';
      case 'Admin':
        return 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200';
      default:
        return 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200';
    }
  };

  if (loading) return (
    <div className="flex items-center justify-center h-64">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 dark:border-blue-400"></div>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 p-6">
      <div className="max-w-7xl mx-auto">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Kullanıcılar</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Kullanıcıları, rollerini ve şifrelerini yönetin</p>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead className="bg-gray-50 dark:bg-gray-700">
                <tr>
                  <th className="px-6 py-4 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Kullanıcı</th>
                  <th className="px-6 py-4 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Email</th>
                  <th className="px-6 py-4 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Mevcut Rol</th>
                  <th className="px-6 py-4 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Yeni Rol</th>
                  <th className="px-6 py-4 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">İşlemler</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                {users.length === 0 ? (
                  <tr>
                    <td colSpan="5" className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                      <div className="text-4xl mb-2">👥</div>
                      <p>Henüz kullanıcı bulunmuyor</p>
                    </td>
                  </tr>
                ) : (
                  users.map((userItem) => (
                    <tr key={userItem.id} className="hover:bg-gray-50 dark:hover:bg-gray-700 transition duration-150">
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 rounded-full bg-blue-100 dark:bg-blue-900 flex items-center justify-center text-blue-600 dark:text-blue-300 font-bold">
                            {userItem.firstName?.charAt(0)}{userItem.lastName?.charAt(0)}
                          </div>
                          <div>
                            <div className="font-medium text-gray-900 dark:text-white">
                              {userItem.firstName} {userItem.lastName}
                            </div>
                            <div className="text-xs text-gray-500 dark:text-gray-400">
                              ID: {userItem.id}
                            </div>
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4 text-gray-600 dark:text-gray-300">
                        {userItem.email}
                      </td>
                      <td className="px-6 py-4">
                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getRoleBadgeColor(userItem.role)}`}>
                          {userItem.role || 'User'}
                        </span>
                      </td>
                      <td className="px-6 py-4">
                        {userItem.email !== 'systemadmin@crm.com' ? (
                          <select
                            value={selectedRoles[userItem.id] || userItem.role || 'User'}
                            onChange={(e) => handleRoleChange(userItem.id, e.target.value)}
                            className="p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white text-sm focus:ring-2 focus:ring-blue-500"
                          >
                            {roles.map((role) => (
                              <option key={role.id} value={role.name}>{role.name}</option>
                            ))}
                          </select>
                        ) : (
                          <span className="text-sm text-gray-400">Değiştirilemez</span>
                        )}
                      </td>
                      <td className="px-6 py-4 text-center">
                        <div className="flex justify-center gap-2">
                          {/* Rol Değiştir Butonu */}
                          {userItem.email !== 'systemadmin@crm.com' && (
                            <button
                              onClick={() => handleSaveRole(userItem.id, userItem.role)}
                              className="px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-xs transition duration-200"
                              title="Rol Değiştir"
                            >
                              👑 Rol Değiştir
                            </button>
                          )}
                          
                          {/* Şifre Değiştir Butonu - Herkes için göster, backend yetki kontrolü yapacak */}
                          <button
                            onClick={() => handleOpenPasswordModal(userItem)}
                            className="px-3 py-1.5 bg-purple-600 hover:bg-purple-700 text-white rounded-lg text-xs transition duration-200"
                            title="Şifre Değiştir"
                          >
                            🔑 Şifre Değiştir
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Şifre Değiştirme Modal */}
      {showPasswordModal && selectedUser && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-800 rounded-xl p-6 w-full max-w-md mx-4">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-xl font-bold text-gray-900 dark:text-white">
                Şifre Değiştir
              </h2>
              <button
                onClick={() => setShowPasswordModal(false)}
                className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                ✕
              </button>
            </div>
            
            <p className="text-gray-600 dark:text-gray-400 mb-4">
              <strong>{selectedUser.firstName} {selectedUser.lastName}</strong> kullanıcısının şifresini değiştiriyorsunuz.
            </p>
            
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Yeni Şifre
                </label>
                <input
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white focus:ring-2 focus:ring-purple-500"
                  placeholder="En az 6 karakter"
                  autoComplete="new-password"
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Şifre Tekrar
                </label>
                <input
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white focus:ring-2 focus:ring-purple-500"
                  placeholder="Şifreyi tekrar girin"
                  autoComplete="new-password"
                />
              </div>
            </div>
            
            <div className="flex justify-end gap-3 mt-6">
              <button
                onClick={() => setShowPasswordModal(false)}
                className="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition"
              >
                İptal
              </button>
              <button
                onClick={handleChangePassword}
                disabled={changingPassword}
                className="px-4 py-2 bg-purple-600 hover:bg-purple-700 text-white rounded-lg transition disabled:opacity-50"
              >
                {changingPassword ? 'Değiştiriliyor...' : 'Şifreyi Değiştir'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}