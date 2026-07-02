import { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import { getImageUrl } from '../helpers/imageHelper';

export default function Brands() {
  const { user } = useAuth();
  const [brands, setBrands] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingBrand, setEditingBrand] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [selectedFile, setSelectedFile] = useState(null); // 🔴 YENİ: Seçilen dosyayı tut
  const [formData, setFormData] = useState({ name: '', description: '', logoUrl: '', isActive: true });

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
    fetchBrands();
  }, [page, search, isActiveFilter]);

  const fetchBrands = async () => {
    try {
      setLoading(true);
      const response = await api.get('/Brands', {
        params: { page, pageSize, search, isActive: isActiveFilter }
      });
      setBrands(response.data.data || []);
      setTotalPages(response.data.totalPages || 1);
      setTotalCount(response.data.totalCount || 0);
    } catch (error) {
      toast.error('Markalar yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => {
    setSearch(searchInput);
    setPage(1);
  };

  // 📤 Logo Yükleme (dosya seçildiğinde)
  const handleFileSelect = (file) => {
    if (!file) return;
    
    const allowedTypes = ['image/jpeg', 'image/png', 'image/jpg', 'image/webp', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
      toast.error('Sadece resim dosyaları yüklenebilir (JPEG, PNG, WEBP, GIF)');
      return;
    }
    
    if (file.size > 2 * 1024 * 1024) {
      toast.error('Dosya boyutu 2MB\'dan küçük olmalıdır');
      return;
    }
    
    // Dosyayı kaydet ve önizleme göster
    setSelectedFile(file);
    const previewUrl = URL.createObjectURL(file);
    setFormData(prev => ({ ...prev, logoUrl: previewUrl }));
  };

  // 🚀 Logoyu backend'e yükle
  const uploadLogoToBackend = async (brandId) => {
    if (!selectedFile) return null;
    
    setUploading(true);
    const uploadFormData = new FormData();
    uploadFormData.append('file', selectedFile);
    
    try {
      const response = await api.post(`/Brands/${brandId}/logo`, uploadFormData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      const logoUrl = response.data.logoUrl;
      toast.success('Logo yüklendi');
      return logoUrl;
    } catch (error) {
      console.error('Yükleme hatası:', error);
      toast.error(error.response?.data?.message || 'Logo yüklenemedi');
      return null;
    } finally {
      setUploading(false);
    }
  };

  // 🗑️ Logo Silme
  const handleDeleteLogo = async () => {
    if (!editingBrand && !selectedFile) {
      setFormData(prev => ({ ...prev, logoUrl: '' }));
      setSelectedFile(null);
      toast.success('Logo temizlendi');
      return;
    }
    
    if (!editingBrand && selectedFile) {
      setSelectedFile(null);
      setFormData(prev => ({ ...prev, logoUrl: '' }));
      toast.success('Logo temizlendi');
      return;
    }
    
    const result = await Swal.fire({
      title: 'Logoyu sil?',
      text: 'Marka logosunu silmek istediğinize emin misiniz?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil',
      cancelButtonText: 'İptal'
    });
    
    if (result.isConfirmed) {
      try {
        await api.delete(`/Brands/${editingBrand.id}/logo`);
        setFormData(prev => ({ ...prev, logoUrl: '' }));
        setSelectedFile(null);
        toast.success('Logo silindi');
        fetchBrands();
      } catch (error) {
        toast.error('Logo silinemedi');
      }
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name.trim()) {
      toast.error('Marka adı zorunludur');
      return;
    }

    try {
      if (editingBrand) {
        // Güncelleme işlemi
        await api.put(`/Brands/${editingBrand.id}`, {
          id: editingBrand.id,
          name: formData.name,
          description: formData.description,
          logoUrl: formData.logoUrl?.startsWith('blob:') ? '' : formData.logoUrl,
          isActive: formData.isActive
        });
        
        // Yeni logo seçildiyse yükle
        if (selectedFile && !formData.logoUrl?.startsWith('blob:')) {
          await uploadLogoToBackend(editingBrand.id);
        } else if (selectedFile) {
          await uploadLogoToBackend(editingBrand.id);
        }
        
        toast.success('Marka başarıyla güncellendi');
      } else {
        // Yeni marka oluştur
        const response = await api.post('/Brands', {
          name: formData.name,
          description: formData.description,
          logoUrl: '', // Önce boş gönder
          isActive: true
        });
        
        const newBrand = response.data;
        
        // Logo seçildiyse yükle
        if (selectedFile) {
          const logoUrl = await uploadLogoToBackend(newBrand.id);
          if (logoUrl) {
            // Logo URL'ini güncelle
            await api.put(`/Brands/${newBrand.id}`, {
              id: newBrand.id,
              name: formData.name,
              description: formData.description,
              logoUrl: logoUrl,
              isActive: true
            });
          }
        }
        
        toast.success('Marka başarıyla eklendi');
      }
      
      setShowModal(false);
      setEditingBrand(null);
      setSelectedFile(null);
      setFormData({ name: '', description: '', logoUrl: '', isActive: true });
      fetchBrands();
    } catch (error) {
      toast.error(error.response?.data?.message || 'Bir hata oluştu');
    }
  };

  const handleDelete = async (id, name) => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${name} markasını silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        const response = await api.delete(`/Brands/${id}`);
        if (response.data.isHardDeleted) {
          toast.success('Marka tamamen silindi');
        } else if (response.data.isSoftDeleted) {
          toast.warning(`Marka pasif hale getirildi. (${response.data.productCount} ürün bağlı olduğu için)`, { duration: 4000 });
        }
        fetchBrands();
      } catch (error) {
        toast.error(error.response?.data?.message || 'Silme hatası');
      }
    }
  };

  const handleEdit = (brand) => {
    setEditingBrand(brand);
    setSelectedFile(null);
    setFormData({
      name: brand.name,
      description: brand.description || '',
      logoUrl: brand.logoUrl || '',
      isActive: brand.isActive
    });
    setShowModal(true);
  };

  const handleActivate = async (id) => {
    try {
      await api.post(`/Brands/${id}/activate`);
      toast.success('Marka aktifleştirildi');
      fetchBrands();
    } catch (error) {
      toast.error('İşlem başarısız');
    }
  };

  const handleDeactivate = async (id) => {
    try {
      await api.post(`/Brands/${id}/deactivate`);
      toast.success('Marka pasifleştirildi');
      fetchBrands();
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
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Markalar</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} marka</p>
        </div>
        <button
          onClick={() => {
            setEditingBrand(null);
            setSelectedFile(null);
            setFormData({ name: '', description: '', logoUrl: '', isActive: true });
            setShowModal(true);
          }}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
        >
          + Yeni Marka
        </button>
      </div>

      {/* Filtreler */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4 mb-6">
        <div className="flex flex-wrap gap-3">
          <input
            type="text"
            placeholder="Marka adı ara..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
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
          <button onClick={handleSearch} className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700">
            Ara
          </button>
          <button onClick={() => {
            setSearchInput('');
            setSearch('');
            setIsActiveFilter('');
            setPage(1);
          }} className="bg-gray-500 text-white px-4 py-2 rounded-lg hover:bg-gray-600">
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
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Logo</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Marka Adı</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Açıklama</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">Durum</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {brands.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-gray-500">
                    Marka bulunmuyor
                  </td>
                </tr>
              ) : (
                brands.map((brand) => (
                  <tr key={brand.id} className="border-t hover:bg-gray-50 transition-colors">
                    <td className="px-6 py-4">
                      {brand.logoUrl ? (
                        <img 
                          src={getImageUrl(brand.logoUrl)} 
                          alt={brand.name} 
                          className="w-10 h-10 object-contain rounded-lg"
                          onError={(e) => { e.target.src = 'https://placehold.co/40x40?text=Logo' }}
                        />
                      ) : (
                        <div className="w-10 h-10 bg-gray-100 rounded-lg flex items-center justify-center text-gray-400">
                          📷
                        </div>
                      )}
                    </td>
                    <td className="px-6 py-4 font-medium">{brand.name}</td>
                    <td className="px-6 py-4 text-gray-600 max-w-xs truncate">{brand.description || '-'}</td>
                    <td className="px-6 py-4 text-center">{getStatusBadge(brand.isActive)}</td>
                    <td className="px-6 py-4 text-center">
                      <div className="flex justify-center gap-2">
                        <button onClick={() => handleEdit(brand)} className="px-3 py-1.5 text-xs border border-indigo-500 text-indigo-600 hover:bg-indigo-500 hover:text-white rounded-lg">
                          ✏️ Düzenle
                        </button>
                        {brand.isActive ? (
                          <button onClick={() => handleDeactivate(brand.id)} className="px-3 py-1.5 text-xs border border-amber-500 text-amber-600 hover:bg-amber-500 hover:text-white rounded-lg">
                            🔒 Pasif
                          </button>
                        ) : (
                          <button onClick={() => handleActivate(brand.id)} className="px-3 py-1.5 text-xs border border-emerald-500 text-emerald-600 hover:bg-emerald-500 hover:text-white rounded-lg">
                            🔓 Aktif
                          </button>
                        )}
                        <button onClick={() => handleDelete(brand.id, brand.name)} className="px-3 py-1.5 text-xs border border-red-500 text-red-600 hover:bg-red-500 hover:text-white rounded-lg">
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
          <div className="text-sm text-gray-500">Toplam {totalCount} kayıt</div>
          <div className="flex gap-2">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="px-3 py-1 border rounded disabled:opacity-50">◀</button>
            <span className="px-3 py-1 text-sm">Sayfa {page} / {totalPages}</span>
            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="px-3 py-1 border rounded disabled:opacity-50">▶</button>
          </div>
        </div>
      )}

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-xl p-6 w-full max-w-md max-h-[90vh] overflow-y-auto">
            <h2 className="text-xl font-bold mb-4">
              {editingBrand ? '✏️ Marka Düzenle' : '➕ Yeni Marka'}
            </h2>
            <form onSubmit={handleSubmit}>
              <input
                type="text"
                placeholder="Marka Adı *"
                value={formData.name}
                onChange={(e) => setFormData({...formData, name: e.target.value})}
                className="w-full p-2 border rounded-lg mb-3"
                required
              />
              <textarea
                placeholder="Açıklama"
                value={formData.description}
                onChange={(e) => setFormData({...formData, description: e.target.value})}
                className="w-full p-2 border rounded-lg mb-3"
                rows="3"
              />
              
              {/* Logo Yükleme Alanı */}
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Marka Logosu
                </label>
                
                {/* Mevcut Logo Gösterimi veya Önizleme */}
                {formData.logoUrl && (
                  <div className="mb-3 flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
                    <img 
                      src={formData.logoUrl} 
                      alt="Logo" 
                      className="w-12 h-12 object-contain rounded" 
                    />
                    <div className="flex-1">
                      <p className="text-xs text-gray-500 truncate">
                        {formData.logoUrl.startsWith('blob:') ? 'Yeni seçilen resim' : formData.logoUrl}
                      </p>
                      <button
                        type="button"
                        onClick={handleDeleteLogo}
                        className="text-xs text-red-500 hover:text-red-600 mt-1"
                      >
                        🗑️ Logoyu Kaldır
                      </button>
                    </div>
                  </div>
                )}
                
                {/* Dosya Seç Butonu */}
                <div className="flex gap-2 mb-2">
                  <label className={`flex-1 flex items-center justify-center gap-2 px-4 py-2 border-2 border-dashed rounded-lg cursor-pointer transition-all ${uploading ? 'bg-gray-100' : 'border-blue-400 hover:bg-blue-50'}`}>
                    <input
                      type="file"
                      accept="image/*"
                      onChange={(e) => handleFileSelect(e.target.files[0])}
                      className="hidden"
                      disabled={uploading}
                    />
                    {uploading ? (
                      <>
                        <div className="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
                        <span>Yükleniyor...</span>
                      </>
                    ) : (
                      <>
                        📁 <span>Resim Seç</span>
                      </>
                    )}
                  </label>
                </div>
                
                <p className="text-xs text-gray-400 mb-2">Önerilen: Kare format, max 2MB (JPEG, PNG, WEBP)</p>
                
                {/* VEYA Ayracı */}
                <div className="relative my-2">
                  <div className="absolute inset-0 flex items-center">
                    <div className="w-full border-t border-gray-300"></div>
                  </div>
                  <div className="relative flex justify-center text-xs">
                    <span className="px-2 bg-white text-gray-500">VEYA</span>
                  </div>
                </div>
                
                {/* URL ile ekleme */}
                <input
                  type="text"
                  placeholder="https://example.com/logo.png"
                  value={formData.logoUrl?.startsWith('blob:') ? '' : formData.logoUrl || ''}
                  onChange={(e) => {
                    setSelectedFile(null);
                    setFormData({...formData, logoUrl: e.target.value});
                  }}
                  className="w-full p-2 border rounded-lg"
                />
              </div>

              {editingBrand && (
                <label className="flex items-center gap-2 mb-3">
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({...formData, isActive: e.target.checked})}
                    className="w-4 h-4"
                  />
                  <span>Aktif</span>
                </label>
              )}
              
              <div className="flex justify-end gap-2 mt-4">
                <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border rounded-lg hover:bg-gray-100">
                  İptal
                </button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700" disabled={uploading}>
                  {uploading ? 'Yükleniyor...' : (editingBrand ? 'Güncelle' : 'Kaydet')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}