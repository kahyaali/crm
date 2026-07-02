import { useState, useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import signalRService from '../services/signalRService';

// ============= AVATAR UPLOADER BİLEŞENİ =============
function AvatarUploader({ personelId, avatarUrl, onAvatarUpdate }) {
  const { user } = useAuth();
  const [uploading, setUploading] = useState(false);
  const [currentAvatar, setCurrentAvatar] = useState(avatarUrl);
  const [showDropdown, setShowDropdown] = useState(false);
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const fileInputRef = useRef(null);
  const dropdownRef = useRef(null);

  const canEdit = user?.role === 'SystemAdmin' || user?.role === 'Admin';

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
      const response = await api.post(`/Personels/${personelId}/avatar`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      
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

  const deleteAvatar = () => {
    setCurrentAvatar(null);
    toast.success('🗑️ Avatar silindi');
    if (onAvatarUpdate) onAvatarUpdate(null);
    setShowConfirmModal(false);
    
    if (personelId) {
      api.delete(`/Personels/${personelId}/avatar`).catch(() => {});
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
                src={currentAvatar} 
                alt="Avatar" 
                className="w-10 h-10 rounded-full object-cover border-2 border-gray-300 dark:border-gray-600 group-hover:border-blue-500 transition-all shadow-md"
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
            <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center text-xl shadow-md border-2 border-gray-300 dark:border-gray-600">
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

// ============= ANA PERSONEL BİLEŞENİ =============
export default function Personels() {
  const { user } = useAuth();
  
  // ===== STATE'LER =====
  const [personels, setPersonels] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [positions, setPositions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [showUserModal, setShowUserModal] = useState(false);
  const [editingPersonel, setEditingPersonel] = useState(null);
  const [selectedPersonel, setSelectedPersonel] = useState(null);
  const [userPassword, setUserPassword] = useState('');
  
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [positionId, setPositionId] = useState('');
  const [isActiveFilter, setIsActiveFilter] = useState('');
  const pageSize = 10;

  const [formData, setFormData] = useState({
    firstName: '', lastName: '', email: '', phone: '',
    address: '', city: '', district: '', postalCode: '',
    departmentId: '', positionId: '', salary: '', currency: 'TRY',
    hireDate: '', managerId: '', createUser: false, password: '',
    personnelNumber: '',
    registrationNumber: ''
  });

  // ===== EXCEL STATE'LERİ =====
  const [uploading, setUploading] = useState(false);
  const [uploadResult, setUploadResult] = useState(null);
  const [uploadProgress, setUploadProgress] = useState({
    current: 0,
    total: 0,
    email: '',
    status: 'idle',
    percentage: 0
  });

  const hasPermission = (permission) => {
    return user?.role === 'SystemAdmin' || user?.role === 'Admin';
  };

  // ========== USEEFFECT'LER ==========
  useEffect(() => {
    fetchPersonels();
    fetchDepartments();
    fetchPositions();
  }, [page, search, departmentId, positionId, isActiveFilter]);

  useEffect(() => {
    if (uploadResult) {
      showUploadResult();
    }
  }, [uploadResult]);


// ========== PROGRESS EVENT DİNLEYİCİSİ ==========
useEffect(() => {
  console.log('🔵 Personel Progress dinleyici bağlandı');

  const handleProgress = (event) => {
    const data = event.detail;
    console.log('📊 Personel Progress Data ALINDI:', data);
    
    if (data) {
      console.log('📊 CurrentRow:', data.currentRow);
      console.log('📊 TotalRows:', data.totalRows);
      console.log('📊 Percentage:', data.percentage);
      
      setUploadProgress({
        current: data.currentRow || 0,
        total: data.totalRows || 0,
        email: data.currentEmail || 'İşleniyor...',
        status: data.status || 'processing',
        percentage: data.percentage || 0
      });

      if (data.status === 'Completed') {
        console.log('✅ Personel yükleme tamamlandı!');
        setTimeout(() => {
          setUploadProgress({ 
            current: 0, 
            total: 0, 
            email: '', 
            status: 'idle', 
            percentage: 0 
          });
          setUploading(false);
        }, 1500);
      }

      if (data.status === 'Error') {
        console.log('❌ Personel yükleme hatası:', data.currentEmail);
        setTimeout(() => {
          setUploadProgress(prev => ({ ...prev, status: 'idle' }));
          setUploading(false);
        }, 3000);
      }
    }
  };

  window.addEventListener('uploadProgress', handleProgress);
  
  return () => {
    console.log('🔴 Personel Progress dinleyici kaldırıldı');
    window.removeEventListener('uploadProgress', handleProgress);
  };
}, []);




  // ========== FETCH FONKSİYONLARI ==========
  const fetchPersonels = async () => {
    try {
      setLoading(true);
      const response = await api.get('/Personels', {
        params: { 
          page, 
          pageSize, 
          search, 
          departmentId, 
          positionId,
          isActive: isActiveFilter 
        }
      });
      setPersonels(response.data.data || []);
      setTotalPages(response.data.totalPages || 1);
      setTotalCount(response.data.totalCount || 0);
    } catch (error) {
      toast.error('Personeller yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const fetchDepartments = async () => {
    try {
      const response = await api.get('/Departments/select-list');  
      setDepartments(response.data || []);
    } catch (error) {
      console.error('Departmanlar yüklenemedi:', error);
      setDepartments([]);
    }
  };

  const fetchPositions = async () => {
    try {
      const response = await api.get('/Positions/select-list');  
      setPositions(response.data || []);
    } catch (error) {
      console.error('Pozisyonlar yüklenemedi:', error);
      setPositions([]);
    }
  };

  // ========== FİLTRE FONKSİYONLARI ==========
  const handleSearch = () => {
    setSearch(searchInput);
    setPage(1);
  };

  const handleClearFilters = () => {
    setSearchInput('');
    setSearch('');
    setDepartmentId('');
    setPositionId('');
    setIsActiveFilter('');
    setPage(1);
  };

  const getFieldName = (field) => {
    const fieldNames = {
      'FirstName': 'Ad',
      'LastName': 'Soyad',
      'Email': 'Email',
      'Phone': 'Telefon',
      'Salary': 'Maaş',
      'HireDate': 'İşe Başlama Tarihi',
      'Password': 'Şifre',
      'DepartmentId': 'Departman',
      'PositionId': 'Pozisyon',
      'ManagerId': 'Yönetici',
      'PersonnelNumber': 'Personel No',
      'RegistrationNumber': 'Sicil No'
    };
    return fieldNames[field] || field;
  };

  const formatSalary = (salary, currency) => {
    if (!salary) return '-';
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency || 'TRY',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(salary);
  };

  // ========== CRUD FONKSİYONLARI ==========
  const handleActivate = async (id) => {
    try {
      await api.post(`/Personels/${id}/activate`);
      toast.success('✅ Personel aktif hale getirildi');
      fetchPersonels();
    } catch (error) {
      toast.error(error.response?.data?.message || 'İşlem başarısız');
    }
  };

  const handleDeactivate = async (id) => {
    try {
      await api.post(`/Personels/${id}/deactivate`);
      toast.success('✅ Personel pasif hale getirildi');
      fetchPersonels();
    } catch (error) {
      toast.error(error.response?.data?.message || 'İşlem başarısız');
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    const submitData = {
      id: editingPersonel ? editingPersonel.id : 0,
      firstName: formData.firstName,
      lastName: formData.lastName,
      email: formData.email,
      phone: formData.phone || null,
      address: formData.address || null,
      city: formData.city || null,
      district: formData.district || null,
      postalCode: formData.postalCode || null,
      departmentId: formData.departmentId ? parseInt(formData.departmentId) : null,
      positionId: formData.positionId ? parseInt(formData.positionId) : null,
      managerId: formData.managerId ? parseInt(formData.managerId) : null,
      salary: formData.salary ? parseFloat(formData.salary) : null,
      currency: formData.currency || 'TRY',
      hireDate: formData.hireDate || null,
      createUser: formData.createUser,
      password: formData.password || null,
      personnelNumber: formData.personnelNumber || null,
      registrationNumber: formData.registrationNumber || null
    };
    
    try {
      if (editingPersonel) {
        await api.put(`/Personels/${editingPersonel.id}`, submitData);
        toast.success('✅ Personel başarıyla güncellendi');
      } else {
        await api.post('/Personels', submitData);
        toast.success('✅ Personel başarıyla eklendi');
        if (formData.createUser) {
          toast.success('✅ Kullanıcı hesabı oluşturuldu');
        }
      }
      setShowModal(false);
      setEditingPersonel(null);
      setFormData({
        firstName: '', lastName: '', email: '', phone: '',
        address: '', city: '', district: '', postalCode: '',
        departmentId: '', positionId: '', salary: '', currency: 'TRY',
        hireDate: '', managerId: '', createUser: false, password: '',
        personnelNumber: '',
        registrationNumber: ''
      });
      fetchPersonels();
    } catch (error) {
      if (error.response?.data?.errors) {
        const errors = error.response.data.errors;
        const firstField = Object.keys(errors)[0];
        const firstMessage = errors[firstField][0];
        const fieldName = getFieldName(firstField);
        toast.error(`❌ ${fieldName}: ${firstMessage}`);
      } else if (error.response?.data?.message) {
        toast.error(`❌ ${error.response.data.message}`);
      } else {
        toast.error('❌ Bir hata oluştu');
      }
    }
  };

  const handleDelete = async (id, name) => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${name} personelini silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        await api.delete(`/Personels/${id}`);
        toast.success('Personel başarıyla silindi');
        fetchPersonels();
      } catch (error) {
        toast.error(error.response?.data?.message || 'Silme hatası');
      }
    }
  };

  const handleEdit = (personel) => {
    setEditingPersonel(personel);
    setFormData({
      firstName: personel.firstName,
      lastName: personel.lastName,
      email: personel.email,
      phone: personel.phone || '',
      address: personel.address || '',
      city: personel.city || '',
      district: personel.district || '',
      postalCode: personel.postalCode || '',
      departmentId: personel.departmentId || '',
      positionId: personel.positionId || '',
      salary: personel.salary || '',
      currency: personel.currency || 'TRY',
      hireDate: personel.hireDate?.split('T')[0] || '',
      managerId: personel.managerId || '',
      createUser: false,
      password: '',
      personnelNumber: personel.personnelNumber || '',
      registrationNumber: personel.registrationNumber || ''
    });
    setShowModal(true);
  };

  const openCreateUserModal = (personel) => {
    setSelectedPersonel(personel);
    setUserPassword('');
    setShowUserModal(true);
  };

  const createUser = async () => {
    if (!userPassword) {
      toast.error('Lütfen şifre girin');
      return;
    }
    try {
      await api.post(`/Personels/${selectedPersonel.id}/create-user`, { password: userPassword });
      toast.success('Kullanıcı başarıyla oluşturuldu');
      setShowUserModal(false);
      setUserPassword('');
      fetchPersonels();
    } catch (error) {
      toast.error(error.response?.data?.message || 'Kullanıcı oluşturulamadı');
    }
  };

  const updatePersonelAvatar = (personelId, newAvatarUrl) => {
    setPersonels(prev => prev.map(p => 
      p.id === personelId ? { ...p, avatarUrl: newAvatarUrl } : p
    ));
  };

  // ========== EXCEL FONKSİYONLARI ==========
  const downloadTemplate = async () => {
    try {
      const response = await api.get('/Personels/download-template', {
        responseType: 'blob'
      });
      
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'Personel_Toplu_Yukleme_Sablonu.xlsx');
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      
      toast.success('✅ Şablon indirildi');
    } catch (error) {
      toast.error('❌ Şablon indirilemedi');
      console.error(error);
    }
  };

const handleExcelUpload = async (e) => {
  const file = e.target.files[0];
  if (!file) return;

  const validTypes = [
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    'application/vnd.ms-excel'
  ];
  
  if (!validTypes.includes(file.type)) {
    toast.error('❌ Lütfen geçerli bir Excel dosyası seçin (.xlsx, .xls)');
    e.target.value = '';
    return;
  }

  if (file.size > 10 * 1024 * 1024) {
    toast.error('❌ Dosya boyutu 10 MB\'dan büyük olamaz');
    e.target.value = '';
    return;
  }

  // ========== 🔥 SIGNALR GRUBA KATIL  ==========
  const uploadId = Date.now().toString();
  
  try {
    if (signalRService.connection && signalRService.connection.state === 'Connected') {
      await signalRService.connection.invoke("JoinUploadGroup", uploadId);
      console.log(`✅ SignalR grubuna katılındı: ${uploadId}`);
    } else {
      console.warn('⚠️ SignalR bağlantısı yok, progress çalışmayabilir');
    }
  } catch (error) {
    console.error('❌ SignalR gruba katılma hatası:', error);
  }
  // ========== BURADA BİTİYOR ==========

  const formData = new FormData();
  formData.append('file', file);


  setUploading(true);
  setUploadResult(null);
  setUploadProgress({
    current: 0,
    total: 0,
    email: 'Başlatılıyor...',
    status: 'processing',
    percentage: 0
  });

  try {
    const response = await api.post('/Personels/upload-excel', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      },
      params: { uploadId: uploadId }
    });

    console.log('✅ Başarılı response:', response.data);

    const data = response.data;
    
    if (data.personel) {
      toast.success(`✅ ${data.personel.firstName} ${data.personel.lastName} başarıyla eklendi!`);
      fetchPersonels();
      
      setUploading(false);
      setUploadProgress({ current: 0, total: 0, email: '', status: 'idle', percentage: 0 });
      e.target.value = '';
      return;
    }
    
    if (data.result) {
      const result = data.result;
      setUploadResult(result);

      // 🔥 BİTTİ - HEMEN KAPAT
      setUploading(false);
      setUploadProgress({ current: 0, total: 0, email: '', status: 'idle', percentage: 0 });

      if (result.errorCount > 0) {
        toast.error(`❌ ${result.errorCount} satır hatalı! Detaylar için modalı inceleyin.`);
      } else {
        toast.success(`✅ ${result.successCount} personel başarıyla eklendi!`);
      }
      
      fetchPersonels();
      e.target.value = '';
      return;
    } else {
      toast.success(data.message || 'İşlem başarılı!');
      setUploading(false);
      setUploadProgress({ current: 0, total: 0, email: '', status: 'idle', percentage: 0 });
      e.target.value = '';
      return;
    }

  } catch (error) {
    console.log('=== HATA DETAYI ===');
    console.log('Status:', error.response?.status);
    console.log('Data:', error.response?.data);
    
    let errorMessage = '❌ Excel yüklenirken hata oluştu';
    
    if (error.response?.data) {
      const data = error.response.data;
      if (data.error) errorMessage += `\n📝 ${data.error}`;
      if (data.message) errorMessage += `\n📝 ${data.message}`;
      if (data.innerException) errorMessage += `\n🔍 ${data.innerException}`;
      if (data.stackTrace) console.log('Stack:', data.stackTrace);
    }
    
    Swal.fire({
      title: '❌ Hata!',
      text: errorMessage,
      icon: 'error',
      confirmButtonText: 'Tamam',
      width: 600
    });
    
    toast.error(errorMessage);
    console.error(error);
    
    setUploading(false);
    setUploadProgress({ current: 0, total: 0, email: '', status: 'idle', percentage: 0 });
    e.target.value = '';
  }
};


  const showUploadResult = () => {
    if (!uploadResult) return;

    const hasErrors = uploadResult.errorCount > 0;
    const errorListHtml = uploadResult.errors && uploadResult.errors.length > 0 ? `
      <hr style="margin: 10px 0; border-color: #e5e7eb;">
      <div style="text-align: left; max-height: 200px; overflow-y: auto;">
        <p style="font-weight: 600; margin-bottom: 8px;">❌ Hatalı Satırlar (${uploadResult.errors.length}):</p>
        <ul style="list-style: none; padding: 0; font-size: 12px;">
          ${uploadResult.errors.map(err => `
            <li style="background: #fef2f2; padding: 6px 10px; margin-bottom: 4px; border-radius: 4px; border-left: 3px solid #ef4444;">
              <strong>Satır ${err.rowNumber}:</strong> ${err.email || 'Email yok'} 
              <span style="color: #dc2626;">- ${err.errorMessage}</span>
            </li>
          `).join('')}
        </ul>
      </div>
    ` : '';

    Swal.fire({
      title: hasErrors ? '⚠️ Toplu Yükleme Tamamlandı' : '✅ Toplu Yükleme Tamamlandı',
      html: `
        <div style="text-align: left; padding: 10px 0;">
          <div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 10px; margin-bottom: 15px;">
            <div style="background: #f3f4f6; padding: 12px; border-radius: 8px; text-align: center;">
              <div style="font-size: 20px; font-weight: bold; color: #374151;">${uploadResult.totalRows}</div>
              <div style="font-size: 12px; color: #6b7280;">📝 Toplam Satır</div>
            </div>
            <div style="background: #d1fae5; padding: 12px; border-radius: 8px; text-align: center;">
              <div style="font-size: 20px; font-weight: bold; color: #065f46;">${uploadResult.successCount}</div>
              <div style="font-size: 12px; color: #065f46;">✅ Başarılı</div>
            </div>
            <div style="background: ${hasErrors ? '#fee2e2' : '#f3f4f6'}; padding: 12px; border-radius: 8px; text-align: center;">
              <div style="font-size: 20px; font-weight: bold; color: ${hasErrors ? '#991b1b' : '#374151'};">${uploadResult.errorCount}</div>
              <div style="font-size: 12px; color: ${hasErrors ? '#991b1b' : '#6b7280'};">❌ Hatalı</div>
            </div>
          </div>
          ${errorListHtml}
        </div>
      `,
      icon: hasErrors ? 'warning' : 'success',
      confirmButtonColor: '#3085d6',
      confirmButtonText: 'Tamam',
      width: 600
    });
  };

  const showDepartmentPositionList = () => {
    const deptList = departments.map(d => `• ${d.name}`).join('\n');
    const posList = positions.map(p => `• ${p.name}`).join('\n');
    
    Swal.fire({
      title: '📋 Sistemdeki Departman ve Pozisyonlar',
      html: `
        <div style="text-align: left; max-height: 500px; overflow-y: auto;">
          <div style="background: #dbeafe; padding: 12px; border-radius: 8px; margin-bottom: 15px;">
            <h4 style="color: #1e40af; margin-bottom: 8px;">🏢 Departmanlar (${departments.length})</h4>
            <p style="font-size: 13px; color: #1e293b; white-space: pre-line; max-height: 200px; overflow-y: auto;">${deptList}</p>
          </div>
          <div style="background: #fce7f3; padding: 12px; border-radius: 8px;">
            <h4 style="color: #9d174d; margin-bottom: 8px;">💼 Pozisyonlar (${positions.length})</h4>
            <p style="font-size: 13px; color: #1e293b; white-space: pre-line; max-height: 200px; overflow-y: auto;">${posList}</p>
          </div>
          <div style="margin-top: 15px; padding: 10px; background: #fef3c7; border-radius: 8px;">
            <p style="font-size: 12px; color: #92400e; text-align: center;">
              ⚠️ Excel'e bu isimleri <strong>BİREBİR</strong> yazın (büyük/küçük harf fark etmez)
            </p>
          </div>
        </div>
      `,
      icon: 'info',
      confirmButtonColor: '#2563eb',
      confirmButtonText: 'Anladım ✅',
      width: 500
    });
  };

  // ========== PROGRESS MODAL ==========
const ProgressModal = () => {
  if (uploadProgress.status === 'idle') return null;

  const isError = uploadProgress.status === 'Error';
  const isCompleted = uploadProgress.status === 'Completed';
  const isProcessing = uploadProgress.status === 'Processing';

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[100]">
      <div className="bg-white dark:bg-gray-800 rounded-2xl p-8 max-w-md w-full mx-4 shadow-2xl">
        <div className="text-center">
          <div className="text-5xl mb-4">
            {isError ? '❌' : isCompleted ? '✅' : '📤'}
          </div>

          <h3 className="text-xl font-bold text-gray-800 dark:text-white mb-2">
            {isError ? 'Yükleme Başarısız' : 
             isCompleted ? '✅ Personel Yükleme Tamamlandı!' : 
             '📤 Personeller Yükleniyor'}
          </h3>

          {isProcessing && (
            <>
              <div className="mb-2">
                <div className="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                  <span className="font-bold text-lg text-blue-600">
                    {uploadProgress.current || 0} / {uploadProgress.total || 0}
                  </span>
                  <span className="font-bold text-lg text-green-600">
                    %{uploadProgress.percentage || 0}
                  </span>
                </div>
                <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-4">
                  <div 
                    className="bg-blue-600 h-4 rounded-full transition-all duration-500 ease-out"
                    style={{ width: `${uploadProgress.percentage || 0}%` }}
                  />
                </div>
              </div>

              <p className="text-sm text-gray-500 dark:text-gray-400 mt-2">
                İşleniyor: <span className="font-medium text-blue-600">{uploadProgress.email || '...'}</span>
              </p>

              <div className="mt-4 flex justify-center">
                <div className="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
              </div>
            </>
          )}

          {(isCompleted || isError) && (
            <div className="mt-4">
              {isCompleted && (
                <p className="text-sm text-green-600 dark:text-green-400">
                  ✅ {uploadProgress.total} personel başarıyla eklendi!
                </p>
              )}
              {isError && (
                <p className="text-sm text-red-600 dark:text-red-400">
                  {uploadProgress.email || 'Bilinmeyen hata oluştu'}
                </p>
              )}
              <button
                onClick={() => {
                  setUploadProgress({ 
                    current: 0, 
                    total: 0, 
                    email: '', 
                    status: 'idle', 
                    percentage: 0 
                  });
                  setUploading(false);
                }}
                className="mt-4 px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors"
              >
                Kapat
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};


  // ========== LOADING ==========
  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 dark:border-blue-400 mx-auto"></div>
      </div>
    );
  }

  // ========== RETURN ==========
  return (
    <div className="container mx-auto px-4 py-8">
      <ProgressModal />

      {/* Header */}
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Personeller</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} personel</p>
        </div>
        
        <div className="flex gap-2 flex-wrap">
          <button
            onClick={showDepartmentPositionList}
            className="bg-gray-600 text-white px-3 py-2 rounded-lg hover:bg-gray-700 transition-colors text-sm flex items-center gap-1"
          >
            📋 Listeler
          </button>

          {hasPermission('personel.create') && (
            <button
              onClick={downloadTemplate}
              className="bg-emerald-600 text-white px-4 py-2 rounded-lg hover:bg-emerald-700 transition-colors flex items-center gap-2"
            >
              📥 Şablon İndir
            </button>
          )}
          
          {hasPermission('personel.create') && (
            <label className={`bg-indigo-600 text-white px-4 py-2 rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer flex items-center gap-2 ${uploading ? 'opacity-50 cursor-not-allowed' : ''}`}>
              {uploading ? (
                <>
                  <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  Yükleniyor...
                </>
              ) : (
                <>
                  📤 Excel Yükle
                </>
              )}
              <input
                type="file"
                accept=".xlsx,.xls"
                onChange={handleExcelUpload}
                className="hidden"
                disabled={uploading}
              />
            </label>
          )}

          {hasPermission('personel.create') && (
            <button
              onClick={() => {
                setEditingPersonel(null);
                setFormData({
                  firstName: '', lastName: '', email: '', phone: '',
                  address: '', city: '', district: '', postalCode: '',
                  departmentId: '', positionId: '', salary: '', currency: 'TRY',
                  hireDate: '', managerId: '', createUser: false, password: '',
                  personnelNumber: '',
                  registrationNumber: ''
                });
                setShowModal(true);
              }}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
            >
              + Yeni Personel
            </button>
          )}
        </div>
      </div>

      {/* Filtreler */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4 mb-6">
        <div className="flex flex-wrap gap-3">
          <input
            type="text"
            placeholder="Ad, Soyad, Email ara..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            className="flex-1 p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400"
          />
          <select
            value={departmentId}
            onChange={(e) => setDepartmentId(e.target.value)}
            className="p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white"
          >
            <option value="">Tüm Departmanlar</option>
            {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
          <select
            value={positionId}
            onChange={(e) => setPositionId(e.target.value)}
            className="p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white"
          >
            <option value="">Tüm Pozisyonlar</option>
            {positions.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
          <select
            value={isActiveFilter}
            onChange={(e) => setIsActiveFilter(e.target.value)}
            className="p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white"
          >
            <option value="">Tüm Durumlar</option>
            <option value="true">✅ Aktif</option>
            <option value="false">❌ Pasif</option>
          </select>
          <button onClick={handleSearch} className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors">
            Ara
          </button>
          <button onClick={handleClearFilters} className="bg-gray-500 text-white px-4 py-2 rounded-lg hover:bg-gray-600 transition-colors">
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
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Avatar</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Ad Soyad</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Personel No</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Sicil No</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Email</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Departman</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Pozisyon</th>
                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Durum</th>
                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {personels.length === 0 ? (
                <tr>
                  <td colSpan={9} className="px-4 py-12 text-center text-gray-500 dark:text-gray-400">
                    Personel bulunmuyor
                  </td>
                </tr>
              ) : (
                personels.map((personel) => (
                  <tr key={personel.id} className="border-t border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                    <td className="px-4 py-3">
                      <AvatarUploader 
                        personelId={personel.id}
                        avatarUrl={personel.avatarUrl}
                        onAvatarUpdate={(newUrl) => updatePersonelAvatar(personel.id, newUrl)}
                      />
                    </td>
                    <td className="px-4 py-3 font-medium text-gray-800 dark:text-white">
                      {personel.firstName} {personel.lastName}
                    </td>
                    <td className="px-4 py-3 text-gray-700 dark:text-gray-300">{personel.personnelNumber || '-'}</td>
                    <td className="px-4 py-3 text-gray-700 dark:text-gray-300">{personel.registrationNumber || '-'}</td>
                    <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">{personel.email}</td>
                    <td className="px-4 py-3 text-gray-700 dark:text-gray-300">{personel.departmentName || '-'}</td>
                    <td className="px-4 py-3 text-gray-700 dark:text-gray-300">{personel.positionName || '-'}</td>
                    <td className="px-4 py-3 text-center">
                      {personel.isActive ? (
                        <span className="px-2 py-1 rounded-full text-xs bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">✅ Aktif</span>
                      ) : (
                        <span className="px-2 py-1 rounded-full text-xs bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200">❌ Pasif</span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-2 flex-wrap">
                        <Link 
                          to={`/personel-detail/${personel.id}`}
                          state={{ from: '/personels' }}
                          className="px-4 py-1.5 bg-emerald-600 text-white text-xs font-semibold hover:bg-emerald-700 transition-colors shadow-sm rounded"
                        >
                          👁️ Detay
                        </Link>
                        
                        {hasPermission('personel.edit') && (
                          <button 
                            onClick={() => handleEdit(personel)} 
                            className="px-4 py-1.5 bg-blue-600 text-white text-xs font-semibold hover:bg-blue-700 transition-colors shadow-sm rounded"
                          >
                            ✏️ Düzenle
                          </button>
                        )}

                        {!personel.userId && hasPermission('personel.createuser') && (
                          <button 
                            onClick={() => openCreateUserModal(personel)} 
                            className="px-4 py-1.5 bg-purple-600 text-white text-xs font-semibold hover:bg-purple-700 transition-colors shadow-sm rounded"
                          >
                            👤 Kullanıcı Aç
                          </button>
                        )}
                        
                        {personel.isActive ? (
                          <button 
                            onClick={() => handleDeactivate(personel.id)} 
                            className="px-4 py-1.5 bg-amber-600 text-white text-xs font-semibold hover:bg-amber-700 transition-colors shadow-sm rounded"
                          >
                            🔒 Pasif
                          </button>
                        ) : (
                          <button 
                            onClick={() => handleActivate(personel.id)} 
                            className="px-4 py-1.5 bg-green-600 text-white text-xs font-semibold hover:bg-green-700 transition-colors shadow-sm rounded"
                          >
                            🔓 Aktif
                          </button>
                        )}
                        
                        {hasPermission('personel.delete') && (
                          <button 
                            onClick={() => handleDelete(personel.id, `${personel.firstName} ${personel.lastName}`)} 
                            className="px-4 py-1.5 bg-rose-600 text-white text-xs font-semibold hover:bg-rose-700 transition-colors shadow-sm rounded"
                          >
                            🗑️ Sil
                          </button>
                        )}
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
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="px-3 py-1 border rounded disabled:opacity-50 dark:border-gray-600 dark:text-white dark:hover:bg-gray-700 transition-colors"
            >
              ◀
            </button>
            <span className="px-3 py-1 text-sm text-gray-700 dark:text-gray-300">Sayfa {page} / {totalPages}</span>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="px-3 py-1 border rounded disabled:opacity-50 dark:border-gray-600 dark:text-white dark:hover:bg-gray-700 transition-colors"
            >
              ▶
            </button>
          </div>
        </div>
      )}

      {/* Personel Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-3xl max-h-[90vh] overflow-y-auto">
            <div className="sticky top-0 bg-white dark:bg-gray-800 px-6 py-4 border-b border-gray-200 dark:border-gray-700">
              <h2 className="text-xl font-bold text-gray-800 dark:text-white">
                {editingPersonel ? 'Personel Düzenle' : 'Yeni Personel'}
              </h2>
            </div>
            <form onSubmit={handleSubmit} className="p-6" noValidate>
              {/* Form içeriği - mevcut haliyle aynı */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <input type="text" placeholder="Ad" value={formData.firstName} onChange={(e) => setFormData({...formData, firstName: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                    <input type="text" placeholder="Soyad" value={formData.lastName} onChange={(e) => setFormData({...formData, lastName: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                  </div>
                  <input type="email" placeholder="Email" value={formData.email} onChange={(e) => setFormData({...formData, email: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                  <input type="tel" placeholder="Telefon" value={formData.phone} onChange={(e) => setFormData({...formData, phone: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                  
                  <input type="text" placeholder="Personel No" value={formData.personnelNumber || ''} onChange={(e) => setFormData({...formData, personnelNumber: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                  <input type="text" placeholder="Sicil No" value={formData.registrationNumber || ''} onChange={(e) => setFormData({...formData, registrationNumber: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                  
                  <select value={formData.departmentId} onChange={(e) => setFormData({...formData, departmentId: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white">
                    <option value="">Departman Seçin</option>
                    {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                  </select>
                  <select value={formData.positionId} onChange={(e) => setFormData({...formData, positionId: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white">
                    <option value="">Pozisyon Seçin</option>
                    {positions.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
                  </select>
                  <select value={formData.managerId} onChange={(e) => setFormData({...formData, managerId: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white">
                    <option value="">Bağlı Olduğu Yönetici</option>
                    {personels.filter(p => p.id !== editingPersonel?.id).map(p => (
                      <option key={p.id} value={p.id}>{p.firstName} {p.lastName}</option>
                    ))}
                  </select>
                </div>
                <div className="space-y-3">
                  <input type="text" placeholder="Adres" value={formData.address} onChange={(e) => setFormData({...formData, address: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                  <div className="grid grid-cols-2 gap-3">
                    <input type="text" placeholder="Şehir" value={formData.city} onChange={(e) => setFormData({...formData, city: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                    <input type="text" placeholder="İlçe" value={formData.district} onChange={(e) => setFormData({...formData, district: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                  </div>
                  <input type="text" placeholder="Posta Kodu" value={formData.postalCode} onChange={(e) => setFormData({...formData, postalCode: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                  <div className="grid grid-cols-2 gap-3">
                    <input type="number" step="0.01" placeholder="Maaş" value={formData.salary} onChange={(e) => setFormData({...formData, salary: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                    <select value={formData.currency} onChange={(e) => setFormData({...formData, currency: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white">
                      <option value="TRY">₺ TL</option>
                      <option value="USD">$ USD</option>
                      <option value="EUR">€ EUR</option>
                      <option value="GBP">£ GBP</option>
                    </select>
                  </div>
                  <input type="date" placeholder="İşe Başlama" value={formData.hireDate} onChange={(e) => setFormData({...formData, hireDate: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white" />
                  {!editingPersonel && (
                    <>
                      <label className="flex items-center gap-2 text-gray-700 dark:text-gray-300">
                        <input type="checkbox" checked={formData.createUser} onChange={(e) => setFormData({...formData, createUser: e.target.checked})} className="w-4 h-4" />
                        <span className="text-sm">Kullanıcı hesabı oluştur</span>
                      </label>
                      {formData.createUser && (
                        <input type="password" placeholder="Şifre" value={formData.password} onChange={(e) => setFormData({...formData, password: e.target.value})} className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
                      )}
                    </>
                  )}
                </div>
              </div>
              <div className="flex justify-end gap-3 mt-6 pt-4 border-t border-gray-200 dark:border-gray-700">
                <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 dark:border-gray-600 dark:text-white transition-colors">
                  İptal
                </button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors">
                  {editingPersonel ? 'Güncelle' : 'Kaydet'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Kullanıcı Oluşturma Modal */}
      {showUserModal && selectedPersonel && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-md">
            <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
              <h2 className="text-xl font-bold text-gray-800 dark:text-white">Kullanıcı Oluştur</h2>
            </div>
            <div className="p-6">
              <p className="mb-4 text-gray-700 dark:text-gray-300">{selectedPersonel.firstName} {selectedPersonel.lastName} için kullanıcı hesabı oluşturulacak.</p>
              <input type="password" placeholder="Şifre" value={userPassword} onChange={(e) => setUserPassword(e.target.value)} className="w-full p-2 border rounded-lg mb-4 dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400" />
              <div className="flex justify-end gap-3">
                <button onClick={() => setShowUserModal(false)} className="px-4 py-2 border rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 dark:border-gray-600 dark:text-white transition-colors">İptal</button>
                <button onClick={createUser} className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors">Oluştur</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
} 