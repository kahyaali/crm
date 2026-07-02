import { useState, useEffect, useRef } from 'react';
import toast from 'react-hot-toast';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';

export default function CompanyLogo() {
  const { hasPermission } = useAuth();
  const [logoUrl, setLogoUrl] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const dropdownRef = useRef(null);
  const fileInputRef = useRef(null);
  const canManage = hasPermission('settings.manage');

  useEffect(() => {
    // Önce backend'den logo'yu çek (öncelikli)
    fetchLogoFromBackend();
    
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setShowDropdown(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const fetchLogoFromBackend = async () => {
    try {
      const response = await api.get('/CompanySettings/logo');
      console.log('Logo API yanıtı:', response.data);
      
      if (response.data?.logoUrl) {
        // API'den gelen URL: /logos/logo_xxx.jpg
        const logoPath = response.data.logoUrl;
        // Tam URL oluştur (backend base URL)
        const baseUrl = import.meta.env.VITE_API_URL || 'https://localhost:7221';
        const fullUrl = logoPath.startsWith('http') ? logoPath : `${baseUrl}${logoPath}`;
        
        console.log('Logo tam URL:', fullUrl);
        setLogoUrl(fullUrl);
      } else {
        setLogoUrl(null);
      }
    } catch (error) {
      console.error('Logo yüklenemedi:', error);
      setLogoUrl(null);
    }
  };

  const handleFileSelect = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      toast.error('Lütfen bir resim dosyası seçin');
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      toast.error('Dosya 2MB\'dan küçük olmalı');
      return;
    }

    setShowDropdown(false);
    
    
    const reader = new FileReader();
    reader.onloadend = () => {
      setLogoUrl(reader.result);
      toast.success('✅ Logo yükleniyor...');
    };
    reader.readAsDataURL(file);
    
   
    saveToBackend(file);
  };

  const saveToBackend = async (file) => {
    setUploading(true);
    const formData = new FormData();
    formData.append('file', file);
    
    try {
      const response = await api.post('/CompanySettings/logo', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      
      if (response.data?.logoUrl) {
        const baseUrl = import.meta.env.VITE_API_URL || 'https://localhost:7221';
        const fullUrl = `${baseUrl}${response.data.logoUrl}`;
        setLogoUrl(fullUrl);
        toast.success('✅ Logo başarıyla kaydedildi!');
      }
    } catch (error) {
      console.error('Backend kayıt hatası:', error);
      toast.error('Logo kaydedilemedi');
    } finally {
      setUploading(false);
    }
  };

  const confirmDelete = () => {
    setShowDropdown(false);
    setShowConfirmModal(true);
  };

  const handleDeleteLogo = async () => {
    setUploading(true);
    try {
      await api.delete('/CompanySettings/logo');
      setLogoUrl(null);
      toast.success('🗑️ Logo başarıyla silindi');
    } catch (error) {
      console.error('Logo silme hatası:', error);
      toast.error('Logo silinemedi');
    } finally {
      setUploading(false);
      setShowConfirmModal(false);
    }
  };


  const DefaultLogo = () => (
    <div className="w-16 h-16 bg-gradient-to-br from-blue-500 to-purple-600 rounded-2xl flex items-center justify-center shadow-lg">
      <span className="text-white font-bold text-3xl">C</span>
    </div>
  );

  return (
    <>
      <div className="relative" ref={dropdownRef}>
        <div 
          className="relative group cursor-pointer"
          onClick={() => canManage && setShowDropdown(!showDropdown)}
        >
          {logoUrl ? (
            <div className="relative">
              <img 
                src={logoUrl} 
                alt="Logo" 
                className="w-16 h-16 object-cover rounded-2xl shadow-lg border-2 border-gray-600 group-hover:border-blue-500 transition-all"
                onError={() => {
                  console.error('Logo yüklenemedi, URL:', logoUrl);
                  setLogoUrl(null);
                }}
              />
              {canManage && (
                <div className="absolute inset-0 bg-black/50 rounded-2xl flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                  <span className="text-white text-2xl">✏️</span>
                </div>
              )}
            </div>
          ) : (
            <DefaultLogo />
          )}
          
          {uploading && (
            <div className="absolute inset-0 bg-black/70 rounded-2xl flex items-center justify-center">
              <div className="w-6 h-6 border-3 border-white border-t-transparent rounded-full animate-spin"></div>
            </div>
          )}
        </div>

        {/* Dropdown Menü */}
        {showDropdown && canManage && (
          <div className="absolute top-20 left-0 bg-gray-800 rounded-xl shadow-2xl w-48 z-50 border border-gray-700 overflow-hidden">
            <div className="py-2">
              <button
                onClick={() => fileInputRef.current?.click()}
                className="w-full px-4 py-2.5 text-left text-sm text-gray-300 hover:bg-gray-700 flex items-center gap-3 transition-colors"
              >
                <span className="text-lg">📷</span> 
                <span>Logo Yükle</span>
              </button>
              
              {logoUrl && (
                <button
                  onClick={confirmDelete}
                  className="w-full px-4 py-2.5 text-left text-sm text-red-400 hover:bg-gray-700 flex items-center gap-3 transition-colors"
                >
                  <span className="text-lg">🗑️</span> 
                  <span>Logoyu Sil</span>
                </button>
              )}
            </div>
          </div>
        )}

        <input
          ref={fileInputRef}
          type="file"
          accept="image/*"
          onChange={handleFileSelect}
          className="hidden"
        />
      </div>

      {/* Silme Onay Modalı */}
      {showConfirmModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[100]">
          <div className="bg-gray-800 rounded-2xl p-6 max-w-sm mx-4 shadow-2xl">
            <div className="text-center">
              <div className="w-20 h-20 bg-red-500/20 rounded-full flex items-center justify-center mx-auto mb-4">
                <span className="text-5xl">🗑️</span>
              </div>
              <h3 className="text-xl font-semibold text-white mb-2">
                Logoyu Sil
              </h3>
              <p className="text-gray-400 mb-6">
                Logoyu silmek istediğine emin misin?<br/>
                Bu işlem geri alınamaz.
              </p>
              <div className="flex gap-3">
                <button
                  onClick={() => setShowConfirmModal(false)}
                  className="flex-1 px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white rounded-xl transition-colors font-medium"
                >
                  İptal
                </button>
                <button
                  onClick={handleDeleteLogo}
                  className="flex-1 px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-xl transition-colors font-medium"
                >
                  Sil
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}