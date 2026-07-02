import { useState, useEffect, useRef } from 'react';
import toast from 'react-hot-toast';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import { getImageUrl } from '../helpers/imageHelper';

export default function AvatarUpload({ personelId, avatarUrl, onAvatarUpdate }) {
  const { hasPermission } = useAuth();
  const [uploading, setUploading] = useState(false);
  const [currentAvatar, setCurrentAvatar] = useState(avatarUrl);
  const [showDropdown, setShowDropdown] = useState(false);
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const fileInputRef = useRef(null);
  const dropdownRef = useRef(null);

 
  //const canEdit = hasPermission('personel.edit');
  
const canEdit = true;  

  useEffect(() => {
    setCurrentAvatar(avatarUrl);
  }, [avatarUrl]);

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setShowDropdown(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

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
      const base64Image = reader.result;
      setCurrentAvatar(base64Image);
      toast.success('✅ Avatar yükleniyor...');
      if (onAvatarUpdate) onAvatarUpdate(base64Image);
      uploadToBackend(file);
    };
    reader.readAsDataURL(file);
  };

  const uploadToBackend = async (file) => {
    const formData = new FormData();
    formData.append('file', file);

    setUploading(true);
    try {
      let response;
      
      if (personelId) {
        response = await api.post(`/Personels/${personelId}/avatar`, formData, {
          headers: { 'Content-Type': 'multipart/form-data' }
        });
      } else {
        response = await api.put('/Personels/my-avatar', formData, {
          headers: { 'Content-Type': 'multipart/form-data' }
        });
      }
      
      if (response.data?.avatarUrl) {
        setCurrentAvatar(response.data.avatarUrl);
        if (onAvatarUpdate) onAvatarUpdate(response.data.avatarUrl);
        toast.success('✅ Avatar kaydedildi!');
      }
    } catch (error) {
      console.error('Backend kayıt hatası:', error);
    } finally {
      setUploading(false);
    }
  };

  const confirmDelete = () => {
    setShowDropdown(false);
    setShowConfirmModal(true);
  };

const deleteAvatar = async () => {
  setCurrentAvatar(null);
  if (onAvatarUpdate) onAvatarUpdate(null);
  setShowConfirmModal(false);
  
  try {
    if (personelId) {
      // Yönetici başkasının avatarını siler
      await api.delete(`/Personels/${personelId}/avatar`);
    } else {
      // Personel kendi avatarını siler
      await api.delete('/Personels/my-avatar');
    }
    toast.success('🗑️ Avatar silindi');
  } catch (error) {
    console.error('Avatar silme hatası:', error);
    toast.error('Avatar silinemedi');
  }
};

  return (
    <>
      <div className="relative" ref={dropdownRef}>
        <div 
          className="relative group cursor-pointer"
          onClick={() => canEdit && setShowDropdown(!showDropdown)}
        >
          {currentAvatar ? (
            <div className="relative">
              <img 
               src={getImageUrl(currentAvatar)}
                alt="Avatar" 
                className="w-10 h-10 rounded-full object-cover border-2 border-gray-300 group-hover:border-blue-500 transition-all shadow-md"
                onError={(e) => {
                  e.target.style.display = 'none';
                  e.target.parentNode.innerHTML = '<div class="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center text-lg shadow-md">👤</div>';
                }}
              />
              {canEdit && (
                <div className="absolute inset-0 bg-black/50 rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                  <span className="text-white text-xs">✏️</span>
                </div>
              )}
            </div>
          ) : (
            <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center text-xl shadow-md border-2 border-gray-300">
              👤
            </div>
          )}
          
          {uploading && (
            <div className="absolute inset-0 bg-black/70 rounded-full flex items-center justify-center">
              <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
            </div>
          )}
        </div>

        {showDropdown && canEdit && !uploading && (
          <div className="absolute top-12 left-0 bg-gray-800 rounded-xl shadow-2xl w-44 z-50 border border-gray-700 overflow-hidden">
            <div className="py-1">
              <button
                onClick={() => fileInputRef.current?.click()}
                className="w-full px-3 py-2 text-left text-xs text-gray-300 hover:bg-gray-700 flex items-center gap-2 transition-colors"
              >
                <span>📷</span> 
                <span>Avatar Yükle</span>
              </button>
              
              {currentAvatar && (
                <button
                  onClick={confirmDelete}
                  className="w-full px-3 py-2 text-left text-xs text-red-400 hover:bg-gray-700 flex items-center gap-2 transition-colors"
                >
                  <span>🗑️</span> 
                  <span>Avatarı Sil</span>
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

      {showConfirmModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[100]">
          <div className="bg-gray-800 rounded-2xl p-6 max-w-sm mx-4">
            <div className="text-center">
              <div className="w-16 h-16 bg-red-500/20 rounded-full flex items-center justify-center mx-auto mb-4">
                <span className="text-4xl">🗑️</span>
              </div>
              <h3 className="text-lg font-semibold text-white mb-2">Avatarı Sil</h3>
              <p className="text-gray-400 mb-5 text-sm">
                Avatarı silmek istediğine emin misin?
              </p>
              <div className="flex gap-3">
                <button
                  onClick={() => setShowConfirmModal(false)}
                  className="flex-1 px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white rounded-xl transition-colors text-sm"
                >
                  İptal
                </button>
                <button
                  onClick={deleteAvatar}
                  className="flex-1 px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-xl transition-colors text-sm"
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