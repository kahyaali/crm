import { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';

export default function ProductCategories() {
  const { user } = useAuth();
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingCategory, setEditingCategory] = useState(null);
  const [formData, setFormData] = useState({ name: '', description: '', parentCategoryId: '', isActive: true });
  const [parentCategories, setParentCategories] = useState([]);

  // Pagination
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [isActiveFilter, setIsActiveFilter] = useState('');
  const pageSize = 10;

  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';

  useEffect(() => {
    fetchCategories();
    fetchParentCategories();
  }, [page, search, isActiveFilter]);

  useEffect(() => {
    if (showModal) {
      fetchParentCategories();
    }
  }, [showModal]);

  const fetchCategories = async () => {
    try {
      setLoading(true);
      const response = await api.get('/ProductCategories', {
        params: { page, pageSize, search, isActive: isActiveFilter }
      });
      setCategories(response.data.data || []);
      setTotalPages(response.data.totalPages || 1);
      setTotalCount(response.data.totalCount || 0);
    } catch (error) {
      toast.error('Kategoriler yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const fetchParentCategories = async () => {
    try {
      const response = await api.get('/ProductCategories/select-list');
      setParentCategories(response.data || []);
    } catch (error) {
      console.error('Üst kategoriler yüklenemedi:', error);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) {
      toast.error('Kategori adı zorunludur');
      return;
    }

    try {
      if (editingCategory) {
        await api.put(`/ProductCategories/${editingCategory.id}`, {
          id: editingCategory.id,
          name: formData.name,
          description: formData.description,
          parentCategoryId: formData.parentCategoryId || null,
          isActive: formData.isActive
        });
        toast.success('Kategori güncellendi');
      } else {
        await api.post('/ProductCategories', {
          name: formData.name,
          description: formData.description,
          parentCategoryId: formData.parentCategoryId || null,
          isActive: true
        });
        toast.success('Kategori eklendi');
      }
      setShowModal(false);
      setEditingCategory(null);
      setFormData({ name: '', description: '', parentCategoryId: '', isActive: true });
      fetchCategories();
      fetchParentCategories();
    } catch (error) {
      toast.error(error.response?.data?.message || 'Bir hata oluştu');
    }
  };

  const handleDelete = async (id, name) => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${name} kategorisini silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        await api.delete(`/ProductCategories/${id}`);
        toast.success('Kategori silindi');
        fetchCategories();
        fetchParentCategories();
      } catch (error) {
        toast.error(error.response?.data?.message || 'Silme hatası');
      }
    }
  };

  const handleEdit = (cat) => {
    setEditingCategory(cat);
    setFormData({
      name: cat.name,
      description: cat.description || '',
      parentCategoryId: cat.parentCategoryId || '',
      isActive: cat.isActive
    });
    setShowModal(true);
  };

  const handleActivate = async (id) => {
    try {
      await api.post(`/ProductCategories/${id}/activate`);
      toast.success('Kategori aktifleştirildi');
      fetchCategories();
      fetchParentCategories();
    } catch (error) {
      toast.error('İşlem başarısız');
    }
  };

  const handleDeactivate = async (id) => {
    try {
      await api.post(`/ProductCategories/${id}/deactivate`);
      toast.success('Kategori pasifleştirildi');
      fetchCategories();
      fetchParentCategories();
    } catch (error) {
      toast.error('İşlem başarısız');
    }
  };

  const getStatusBadge = (isActive) => {
    return isActive ? (
      <span className="px-2 py-1 rounded-full text-xs bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">✅ Aktif</span>
    ) : (
      <span className="px-2 py-1 rounded-full text-xs bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200">❌ Pasif</span>
    );
  };

  const openCreateModal = () => {
    setEditingCategory(null);
    setFormData({ name: '', description: '', parentCategoryId: '', isActive: true });
    fetchParentCategories();
    setShowModal(true);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 dark:border-blue-400 mx-auto"></div>
      </div>
    );
  }

  if (!isAdmin) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <div className="text-6xl mb-4">🔒</div>
          <h2 className="text-2xl font-bold text-gray-800 dark:text-white mb-2">Yetkiniz Yok</h2>
          <p className="text-gray-500 dark:text-gray-400">Bu sayfaya erişim yetkiniz bulunmamaktadır.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Ürün Kategorileri</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} kategori</p>
        </div>
        <button
          onClick={openCreateModal}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
        >
          + Yeni Kategori
        </button>
      </div>

      {/* Filtreler */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4 mb-6">
        <div className="flex flex-wrap gap-3">
          <input
            type="text"
            placeholder="Kategori ara..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && setSearch(searchInput)}
            className="flex-1 p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400"
          />
          <select
            value={isActiveFilter}
            onChange={(e) => setIsActiveFilter(e.target.value)}
            className="p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white"
          >
            <option value="">Tüm Durumlar</option>
            <option value="true">Aktif</option>
            <option value="false">Pasif</option>
          </select>
          <button onClick={() => setSearch(searchInput)} className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors">
            Ara
          </button>
          <button onClick={() => {
            setSearchInput('');
            setSearch('');
            setIsActiveFilter('');
            setPage(1);
          }} className="bg-gray-500 text-white px-4 py-2 rounded-lg hover:bg-gray-600 transition-colors">
            Temizle
          </button>
        </div>
      </div>

      {/* Tablo */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-700">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Kategori Adı</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Açıklama</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Üst Kategori</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Ürün Sayısı</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Durum</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {categories.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                    Kategori bulunmuyor
                  </td>
                </tr>
              ) : (
                categories.map((cat) => (
                  <tr key={cat.id} className="border-t border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                    <td className="px-6 py-4 font-medium text-gray-800 dark:text-white">{cat.name}</td>
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300">{cat.description || '-'}</td>
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300">{cat.parentCategoryName || '-'}</td>
                    <td className="px-6 py-4 text-center text-gray-700 dark:text-gray-300">{cat.productCount || 0}</td>
                    <td className="px-6 py-4 text-center">{getStatusBadge(cat.isActive)}</td>
                    <td className="px-6 py-4 text-center">
                      <div className="flex justify-center gap-2">
                        <button 
                          onClick={() => handleEdit(cat)} 
                          className="px-3 py-1.5 text-xs font-medium border border-indigo-500 text-indigo-600 hover:bg-indigo-500 hover:text-white dark:border-indigo-400 dark:text-indigo-400 dark:hover:bg-indigo-500 dark:hover:text-white transition-all duration-200 rounded"
                        >
                          ✏️ Düzenle
                        </button>
                        
                        {cat.isActive ? (
                          <button 
                            onClick={() => handleDeactivate(cat.id)} 
                            className="px-3 py-1.5 text-xs font-medium border border-gray-500 text-gray-600 hover:bg-gray-500 hover:text-white dark:border-gray-400 dark:text-gray-400 dark:hover:bg-gray-500 dark:hover:text-white transition-all duration-200 rounded"
                          >
                            🔒 Pasif
                          </button>
                        ) : (
                          <button 
                            onClick={() => handleActivate(cat.id)} 
                            className="px-3 py-1.5 text-xs font-medium border border-emerald-500 text-emerald-600 hover:bg-emerald-500 hover:text-white dark:border-emerald-400 dark:text-emerald-400 dark:hover:bg-emerald-500 dark:hover:text-white transition-all duration-200 rounded"
                          >
                            🔓 Aktif
                          </button>
                        )}
                        
                        <button 
                          onClick={() => handleDelete(cat.id, cat.name)} 
                          className="px-3 py-1.5 text-xs font-medium border border-red-500 text-red-600 hover:bg-red-500 hover:text-white dark:border-red-400 dark:text-red-400 dark:hover:bg-red-500 dark:hover:text-white transition-all duration-200 rounded"
                        >
                          🗑️ Sil
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

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex justify-between items-center mt-4">
          <div className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} kayıt</div>
          <div className="flex gap-2">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="px-3 py-1 border rounded disabled:opacity-50 dark:border-gray-600 dark:text-white dark:hover:bg-gray-700 transition-colors">
              ◀
            </button>
            <span className="px-3 py-1 text-sm text-gray-700 dark:text-gray-300">Sayfa {page} / {totalPages}</span>
            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="px-3 py-1 border rounded disabled:opacity-50 dark:border-gray-600 dark:text-white dark:hover:bg-gray-700 transition-colors">
              ▶
            </button>
          </div>
        </div>
      )}

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 w-full max-w-md">
            <h2 className="text-xl font-bold text-gray-800 dark:text-white mb-4">{editingCategory ? 'Kategori Düzenle' : 'Yeni Kategori'}</h2>
            <form onSubmit={handleSubmit} noValidate>
              <input 
                type="text" 
                placeholder="Kategori Adı" 
                value={formData.name} 
                onChange={(e) => setFormData({...formData, name: e.target.value})} 
                className="w-full p-2 border rounded mb-3 dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" 
                required 
              />
              <textarea 
                placeholder="Açıklama" 
                value={formData.description} 
                onChange={(e) => setFormData({...formData, description: e.target.value})} 
                className="w-full p-2 border rounded mb-3 dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" 
                rows="3" 
              />
              <select 
                value={formData.parentCategoryId} 
                onChange={(e) => setFormData({...formData, parentCategoryId: e.target.value})} 
                className="w-full p-2 border rounded mb-3 dark:bg-gray-700 dark:border-gray-600 dark:text-white"
              >
                <option value="">Üst Kategori Yok</option>
                {parentCategories.filter(c => c.id !== editingCategory?.id).map(c => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
              {editingCategory && (
                <label className="flex items-center gap-2 mb-3 text-gray-700 dark:text-gray-300">
                  <input type="checkbox" checked={formData.isActive} onChange={(e) => setFormData({...formData, isActive: e.target.checked})} className="w-4 h-4" />
                  <span>Aktif</span>
                </label>
              )}
              <div className="flex justify-end gap-2">
                <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border rounded hover:bg-gray-100 dark:hover:bg-gray-700 dark:border-gray-600 dark:text-white transition-colors">
                  İptal
                </button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors">
                  Kaydet
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}