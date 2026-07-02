// pages/Departments.jsx
import { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';

export default function Departments() {
  const { user } = useAuth();
  const [departments, setDepartments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingDepartment, setEditingDepartment] = useState(null);
  const [formData, setFormData] = useState({ name: '', description: '' });
  
  // Pagination States
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const pageSize = 10;

  const isSuperAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
  const canViewPage = isSuperAdmin;
  const canCreateOrEdit = isSuperAdmin;
  const canDeleteOrToggle = isSuperAdmin;

  useEffect(() => {
    if (canViewPage) {
      fetchDepartments();
    }
  }, [page, search]);

  const fetchDepartments = async () => {
    try {
      setLoading(true);
      const url = isSuperAdmin ? '/Departments/all' : '/Departments';
      const response = await api.get(url, {
        params: { page, pageSize, search }
      });
      
      setDepartments(response.data.data);
      setTotalPages(response.data.totalPages);
      setTotalCount(response.data.totalCount);
    } catch (error) {
      const message = error.response?.data?.message || 'Departmanlar yüklenemedi';
      toast.error(message);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => {
    setSearch(searchInput);
    setPage(1);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) {
      toast.error('Departman adı zorunludur');
      return;
    }

    try {
      if (editingDepartment) {
        await api.put(`/Departments/${editingDepartment.id}`, {
          name: formData.name.trim(),
          description: formData.description?.trim() || null
        });
        toast.success('Departman başarıyla güncellendi');
      } else {
        await api.post('/Departments', {
          name: formData.name.trim(),
          description: formData.description?.trim() || null
        });
        toast.success('Departman başarıyla eklendi');
      }
      setShowModal(false);
      setEditingDepartment(null);
      setFormData({ name: '', description: '' });
      fetchDepartments();
    } catch (error) {
      const message = error.response?.data?.message || 'Bir hata oluştu';
      toast.error(message);
    }
  };

  const handleDelete = async (id, name) => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${name} departmanını silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        const response = await api.delete(`/Departments/${id}`);
        toast.success(response.data.message || 'Departman tamamen silindi');
        fetchDepartments();
      } catch (error) {
        const message = error.response?.data?.message || 'Silme hatası';
        toast.error(message);
      }
    }
  };

  const handleDeactivate = async (id, name) => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${name} departmanını pasif hale getirmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#f59e0b',
      confirmButtonText: 'Evet, pasif yap!',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        const response = await api.post(`/Departments/${id}/deactivate`);
        toast.success(response.data.message || 'Departman pasif hale getirildi');
        fetchDepartments();
      } catch (error) {
        const message = error.response?.data?.message || 'İşlem başarısız';
        toast.error(message);
      }
    }
  };

  const handleActivate = async (id, name) => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${name} departmanını aktif hale getirmek istediğinize emin misiniz?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#10b981',
      confirmButtonText: 'Evet, aktif yap!',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        const response = await api.post(`/Departments/${id}/activate`);
        toast.success(response.data.message || 'Departman aktif hale getirildi');
        fetchDepartments();
      } catch (error) {
        const message = error.response?.data?.message || 'İşlem başarısız';
        toast.error(message);
      }
    }
  };

  const handleEdit = (dept) => {
    setEditingDepartment(dept);
    setFormData({
      name: dept.name,
      description: dept.description || ''
    });
    setShowModal(true);
  };

  const goToPage = (newPage) => {
    setPage(Math.max(1, Math.min(totalPages, newPage)));
  };

  if (loading) return (
    <div className="flex items-center justify-center h-64">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
    </div>
  );

  if (!canViewPage) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <div className="text-6xl mb-4">🔒</div>
          <h2 className="text-2xl font-bold text-gray-800 dark:text-white">Yetkiniz Yok</h2>
          <p className="text-gray-500 mt-2">Bu sayfaya erişim yetkiniz bulunmamaktadır.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-6 gap-4">
        <div>
          <h1 className="text-2xl md:text-3xl font-bold text-gray-800 dark:text-white">Departmanlar</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Toplam <span className="font-semibold text-blue-600">{totalCount}</span> departman
          </p>
        </div>
        {canCreateOrEdit && (
          <button
            onClick={() => {
              setEditingDepartment(null);
              setFormData({ name: '', description: '' });
              setShowModal(true);
            }}
            className="bg-blue-600 hover:bg-blue-700 text-white px-5 py-2.5 rounded-lg transition duration-200 flex items-center gap-2 shadow-sm"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Yeni Departman
          </button>
        )}
      </div>

      {/* Arama */}
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4 mb-6">
        <div className="flex flex-col md:flex-row gap-3">
          <div className="flex-1">
            <input
              type="text"
              placeholder="Departman adı ile ara..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
              className="w-full p-2.5 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
            />
          </div>
          <div className="flex gap-2">
            <button
              onClick={handleSearch}
              className="bg-blue-600 hover:bg-blue-700 text-white px-5 py-2.5 rounded-lg transition duration-200 flex items-center gap-2"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              Ara
            </button>
            <button
              onClick={() => {
                setSearchInput('');
                setSearch('');
                setPage(1);
              }}
              className="bg-gray-500 hover:bg-gray-600 text-white px-5 py-2.5 rounded-lg transition duration-200"
            >
              Temizle
            </button>
          </div>
        </div>
      </div>

      {/* Tablo */}
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-700">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Departman</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Açıklama</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Personel</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">Durum</th>
                {(canCreateOrEdit || canDeleteOrToggle) && (
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">İşlemler</th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {departments.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                    <div className="text-4xl mb-2">📁</div>
                    <p>Henüz departman bulunmuyor</p>
                    {canCreateOrEdit && (
                      <button
                        onClick={() => setShowModal(true)}
                        className="mt-3 text-blue-600 hover:text-blue-700 font-medium"
                      >
                        + İlk departmanı ekle
                      </button>
                    )}
                  </td>
                </tr>
              ) : (
                departments.map((dept) => (
                  <tr key={dept.id} className="hover:bg-gray-50 dark:hover:bg-gray-700 transition duration-150">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        <div className="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900 flex items-center justify-center text-blue-600 dark:text-blue-300">
                          📁
                        </div>
                        <span className="font-medium text-gray-800 dark:text-white">{dept.name}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-300 max-w-xs truncate">
                      {dept.description || '-'}
                    </td>
                    <td className="px-6 py-4 text-center">
                      <span className="inline-flex items-center justify-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300">
                        {dept.personelCount} kişi
                      </span>
                    </td>
                    <td className="px-6 py-4 text-center">
                      {dept.isActive ? (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-300">
                          <span className="w-1.5 h-1.5 rounded-full bg-green-500"></span>
                          Aktif
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300">
                          <span className="w-1.5 h-1.5 rounded-full bg-red-500"></span>
                          Pasif
                        </span>
                      )}
                    </td>
                    {(canCreateOrEdit || canDeleteOrToggle) && (
                      <td className="px-6 py-4">
                        <div className="flex items-center justify-center gap-2">
                          {canCreateOrEdit && (
                            <button
                              onClick={() => handleEdit(dept)}
                              className="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-blue-700 bg-blue-100 rounded-lg hover:bg-blue-200 transition duration-200"
                              title="Düzenle"
                            >
                              ✏️ Düzenle
                            </button>
                          )}
                          {canDeleteOrToggle && (
                            <>
                              {dept.isActive ? (
                                <button
                                  onClick={() => handleDeactivate(dept.id, dept.name)}
                                  className="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-orange-700 bg-orange-100 rounded-lg hover:bg-orange-200 transition duration-200"
                                  title="Pasif Yap"
                                >
                                  ⚫ Pasif Yap
                                </button>
                              ) : (
                                <button
                                  onClick={() => handleActivate(dept.id, dept.name)}
                                  className="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-green-700 bg-green-100 rounded-lg hover:bg-green-200 transition duration-200"
                                  title="Aktif Yap"
                                >
                                  🟢 Aktif Yap
                                </button>
                              )}
                              <button
                                onClick={() => handleDelete(dept.id, dept.name)}
                                className="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-red-700 bg-red-100 rounded-lg hover:bg-red-200 transition duration-200"
                                title="Sil"
                              >
                                🗑️ Sil
                              </button>
                            </>
                          )}
                        </div>
                      </td>
                    )}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex flex-col sm:flex-row justify-between items-center mt-6 gap-4">
          <div className="text-sm text-gray-500 dark:text-gray-400">
            Toplam <span className="font-semibold">{totalCount}</span> kayıt
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => goToPage(1)}
              disabled={page === 1}
              className="px-3 py-2 text-sm font-medium border border-gray-300 dark:border-gray-600 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700 transition"
            >
              ⏮ İlk
            </button>
            <button
              onClick={() => goToPage(page - 1)}
              disabled={page === 1}
              className="px-3 py-2 text-sm font-medium border border-gray-300 dark:border-gray-600 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700 transition"
            >
              ◀ Önceki
            </button>
            <span className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300">
              Sayfa {page} / {totalPages}
            </span>
            <button
              onClick={() => goToPage(page + 1)}
              disabled={page === totalPages}
              className="px-3 py-2 text-sm font-medium border border-gray-300 dark:border-gray-600 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700 transition"
            >
              Sonraki ▶
            </button>
            <button
              onClick={() => goToPage(totalPages)}
              disabled={page === totalPages}
              className="px-3 py-2 text-sm font-medium border border-gray-300 dark:border-gray-600 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700 transition"
            >
              Son ⏭
            </button>
          </div>
        </div>
      )}

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-md">
            <div className="flex justify-between items-center p-6 border-b border-gray-200 dark:border-gray-700">
              <h2 className="text-xl font-bold text-gray-800 dark:text-white">
                {editingDepartment ? 'Departman Düzenle' : 'Yeni Departman'}
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
            <form onSubmit={handleSubmit} className="p-6" noValidate>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Departman Adı <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  placeholder="Örn: İnsan Kaynakları"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full p-2.5 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
                  required
                />
              </div>
              <div className="mb-6">
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Açıklama (İsteğe bağlı)
                </label>
                <textarea
                  placeholder="Departman hakkında kısa bir açıklama"
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
                  {editingDepartment ? 'Güncelle' : 'Kaydet'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}