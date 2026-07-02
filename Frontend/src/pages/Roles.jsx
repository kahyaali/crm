import { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';

export default function Roles() {
  const [roles, setRoles] = useState([]);
  const [permissions, setPermissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [showPermModal, setShowPermModal] = useState(false);
  const [editingRole, setEditingRole] = useState(null);
  const [selectedRole, setSelectedRole] = useState(null);
  const [selectedPermissions, setSelectedPermissions] = useState([]);
  const [formData, setFormData] = useState({ name: '', description: '' });

  useEffect(() => {
    fetchRoles();
    fetchPermissions();
  }, []);

  const fetchRoles = async () => {
    try {
      const response = await api.get('/Roles');
      setRoles(response.data);
    } catch (error) {
      toast.error('Roller yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const fetchPermissions = async () => {
    try {
      const response = await api.get('/Permissions');
      setPermissions(response.data);
    } catch (error) {
      console.error('Yetkiler yüklenemedi:', error);
    }
  };

  const fetchRolePermissions = async (roleId) => {
    try {
      const response = await api.get(`/Roles/${roleId}/permissions`);
      return response.data.map(p => p.id);
    } catch (error) {
      console.error('Rol yetkileri yüklenemedi:', error);
      return [];
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) {
      toast.error('Rol adı zorunludur');
      return;
    }

    try {
      if (editingRole) {
        await api.put(`/Roles/${editingRole.id}`, formData);
        toast.success('Rol başarıyla güncellendi');
      } else {
        await api.post('/Roles', formData);
        toast.success('Rol başarıyla oluşturuldu');
      }
      setShowModal(false);
      setEditingRole(null);
      setFormData({ name: '', description: '' });
      fetchRoles();
    } catch (error) {
      toast.error(error.response?.data?.message || 'Bir hata oluştu');
    }
  };

  const handleDelete = async (role) => {
    if (role.name === 'SystemAdmin') {
      toast.error('SystemAdmin rolü silinemez');
      return;
    }

    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${role.name} rolünü silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        await api.delete(`/Roles/${role.id}`);
        toast.success('Rol başarıyla silindi');
        fetchRoles();
      } catch (error) {
        toast.error(error.response?.data?.message || 'Silme hatası');
      }
    }
  };

  const openPermissionModal = async (role) => {
    setSelectedRole(role);
    const rolePerms = await fetchRolePermissions(role.id);
    setSelectedPermissions(rolePerms);
    setShowPermModal(true);
  };

  const handleAssignPermissions = async () => {
    try {
      await api.post(`/Roles/${selectedRole.id}/permissions`, {
        permissionIds: selectedPermissions
      });
      toast.success('Yetkiler başarıyla güncellendi');
      setShowPermModal(false);
      fetchRoles();
    } catch (error) {
      toast.error(error.response?.data?.message || 'Yetki atama hatası');
    }
  };

  const togglePermission = (permId) => {
    setSelectedPermissions(prev =>
      prev.includes(permId)
        ? prev.filter(id => id !== permId)
        : [...prev, permId]
    );
  };

  if (loading) return (
    <div className="flex items-center justify-center h-64">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Roller ve Yetkiler</h1>
              <p className="text-gray-500 dark:text-gray-400 mt-1">Rolleri yönetin ve yetkileri atayın</p>
            </div>
            <button
              onClick={() => {
                setEditingRole(null);
                setFormData({ name: '', description: '' });
                setShowModal(true);
              }}
              className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white px-5 py-2.5 rounded-lg transition duration-200 shadow-sm"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              Yeni Rol
            </button>
          </div>
        </div>

        {/* Roller Tablosu */}
        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead className="bg-gray-50 dark:bg-gray-700">
                <tr>
                  <th className="px-6 py-4 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Rol Adı</th>
                  <th className="px-6 py-4 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Açıklama</th>
                  <th className="px-6 py-4 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">İşlemler</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                {roles.map((role) => (
                  <tr key={role.id} className="hover:bg-gray-50 dark:hover:bg-gray-700 transition duration-150">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        <div className={`w-2 h-2 rounded-full ${role.name === 'SystemAdmin' ? 'bg-red-500' : 'bg-green-500'}`}></div>
                        <span className="font-medium text-gray-900 dark:text-white">{role.name}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-300">{role.description || '-'}</td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => openPermissionModal(role)}
                          disabled={role.name === 'SystemAdmin'}
                          className={`p-2 rounded-lg transition duration-200 ${
                            role.name === 'SystemAdmin'
                              ? 'text-gray-400 cursor-not-allowed'
                              : 'text-purple-600 hover:bg-purple-50 dark:hover:bg-purple-900'
                          }`}
                          title="Yetkiler"
                        >
                          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                          </svg>
                        </button>
                        {role.name !== 'SystemAdmin' && (
                          <>
                            <button
                              onClick={() => {
                                setEditingRole(role);
                                setFormData({ name: role.name, description: role.description || '' });
                                setShowModal(true);
                              }}
                              className="p-2 text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900 rounded-lg transition duration-200"
                              title="Düzenle"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                              </svg>
                            </button>
                            <button
                              onClick={() => handleDelete(role)}
                              className="p-2 text-red-600 hover:bg-red-50 dark:hover:bg-red-900 rounded-lg transition duration-200"
                              title="Sil"
                            >
                              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                              </svg>
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Rol Ekleme/Düzenleme Modal */}
        {showModal && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-md">
              <div className="flex justify-between items-center p-6 border-b border-gray-200 dark:border-gray-700">
                <h2 className="text-xl font-bold text-gray-800 dark:text-white">
                  {editingRole ? 'Rol Düzenle' : 'Yeni Rol'}
                </h2>
                <button
                  onClick={() => setShowModal(false)}
                  className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
              <form onSubmit={handleSubmit} className="p-6">
                <div className="mb-4">
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Rol Adı <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    placeholder="Örn: Satış Müdürü"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="w-full p-2.5 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
                    required
                  />
                </div>
                <div className="mb-6">
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Açıklama
                  </label>
                  <textarea
                    placeholder="Rol hakkında kısa bir açıklama"
                    value={formData.description}
                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                    className="w-full p-2.5 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
                    rows="3"
                  />
                </div>
                <div className="flex justify-end gap-3">
                  <button
                    type="button"
                    onClick={() => setShowModal(false)}
                    className="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition"
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition duration-200"
                  >
                    {editingRole ? 'Güncelle' : 'Kaydet'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Yetki Atama Modal */}
        {showPermModal && selectedRole && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-3xl max-h-[85vh] overflow-hidden">
              <div className="flex justify-between items-center p-6 border-b border-gray-200 dark:border-gray-700">
                <div>
                  <h2 className="text-xl font-bold text-gray-800 dark:text-white">{selectedRole.name} - Yetkiler</h2>
                  <p className="text-sm text-gray-500 mt-1">Bu role atanacak yetkileri seçin</p>
                </div>
                <button
                  onClick={() => setShowPermModal(false)}
                  className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
              <div className="overflow-y-auto p-6" style={{ maxHeight: 'calc(85vh - 140px)' }}>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                  {permissions.map((perm) => (
                    <label
                      key={perm.id}
                      className="flex items-center gap-3 p-3 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer transition duration-150"
                    >
                      <input
                        type="checkbox"
                        checked={selectedPermissions.includes(perm.id)}
                        onChange={() => togglePermission(perm.id)}
                        className="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
                      />
                      <div>
                        <div className="font-medium text-sm text-gray-800 dark:text-white">{perm.name}</div>
                        <div className="text-xs text-gray-400">{perm.module} / {perm.action}</div>
                      </div>
                    </label>
                  ))}
                </div>
              </div>
              <div className="flex justify-end gap-3 p-6 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
                <button
                  onClick={() => setShowPermModal(false)}
                  className="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition"
                >
                  İptal
                </button>
                <button
                  onClick={handleAssignPermissions}
                  className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition duration-200"
                >
                  Yetkileri Kaydet
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}