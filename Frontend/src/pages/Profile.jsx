import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import toast from 'react-hot-toast';
import api from '../services/api';
import AvatarUpload from '../components/AvatarUpload'; 

export default function Profile() {
  const { user, logout } = useAuth();
  const [loading, setLoading] = useState(false);
  const [showChangePassword, setShowChangePassword] = useState(false);
  const [personel, setPersonel] = useState(null);
  
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    address: '',
    city: '',
    district: '',
    postalCode: ''
  });

  useEffect(() => {
    fetchPersonelInfo();
  }, []);

  const fetchPersonelInfo = async () => {
    try {
      const response = await api.get('/Personels/my-info');
      setPersonel(response.data);
      setFormData({
        firstName: response.data.firstName || '',
        lastName: response.data.lastName || '',
        email: response.data.email || '',
        phone: response.data.phone || '',
        address: response.data.address || '',
        city: response.data.city || '',
        district: response.data.district || '',
        postalCode: response.data.postalCode || ''
      });
    } catch (error) {
      console.error('Personel bilgileri alınamadı:', error);
    }
  };

 const handleUpdateProfile = async (e) => {
    e.preventDefault();
    setLoading(true);

    // SADECE GÜNCELLENECEK ALANLAR
    const updateData = {
        firstName: formData.firstName,
        lastName: formData.lastName,
        phone: formData.phone,
        address: formData.address,
        city: formData.city,
        district: formData.district,
        postalCode: formData.postalCode
    };

    console.log('Gönderilen veri:', updateData);
    
    try {
        const response = await api.put('/Personels/my-profile', updateData);
        console.log('Başarılı:', response.data);
        toast.success('Profil bilgileriniz güncellendi');
        
        const userStr = localStorage.getItem('user');
        if (userStr) {
            const userData = JSON.parse(userStr);
            userData.firstName = formData.firstName;
            userData.lastName = formData.lastName;
            localStorage.setItem('user', JSON.stringify(userData));
        }
        
        // Bilgileri yenile
        await fetchPersonelInfo();
        
    } catch (error) {
        console.log('HATA DETAYI:', error.response?.data);
        console.log('STATUS:', error.response?.status);
        
        if (error.response?.data?.errors) {
            const errors = error.response.data.errors;
            for (const [field, messages] of Object.entries(errors)) {
                toast.error(`${field}: ${messages[0]}`);
            }
        } else if (error.response?.data?.message) {
            toast.error(error.response.data.message);
        } else {
            toast.error('Güncelleme başarısız');
        }
    } finally {
        setLoading(false);
    }
};

  const handleChangePassword = async (e) => {
    e.preventDefault();
    
    if (!newPassword || newPassword.length < 6) {
      toast.error('Yeni şifre en az 6 karakter olmalıdır');
      return;
    }
    
    if (newPassword !== confirmPassword) {
      toast.error('Şifreler eşleşmiyor');
      return;
    }
    
    setLoading(true);
    
    try {
      await api.post('/Auth/change-password', {
        currentPassword: currentPassword,
        newPassword: newPassword
      });
      
      toast.success('Şifreniz başarıyla değiştirildi. Lütfen tekrar giriş yapın.');
      
      setTimeout(() => {
        logout();
      }, 2000);
      
    } catch (error) {
      toast.error(error.response?.data?.message || 'Şifre değiştirilemedi');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 p-6">
      <div className="max-w-4xl mx-auto">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Profilim</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Hesap bilgilerinizi yönetin</p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Sol Kolon - Avatar */}
          <div className="lg:col-span-1">
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-6">
              <div className="text-center">
                <div className="flex justify-center mb-4">
                  <AvatarUpload 
                    personelId={personel?.id}
                    avatarUrl={personel?.avatarUrl}
                    onAvatarUpdate={(newUrl) => {
                      setPersonel(prev => ({ ...prev, avatarUrl: newUrl }));
                    }}
                  />
                </div>
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                  {formData.firstName} {formData.lastName}
                </h3>
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                  {user?.role === 'SystemAdmin' ? 'Sistem Yöneticisi' : 
                   user?.role === 'Admin' ? 'Yönetici' : 'Personel'}
                </p>
                <p className="text-xs text-gray-400 mt-2">
                  Kayıt Tarihi: {personel?.createdAt ? new Date(personel.createdAt).toLocaleDateString('tr-TR') : '-'}
                </p>
              </div>
            </div>
          </div>

          {/* Sağ Kolon - Bilgiler */}
          <div className="lg:col-span-2 space-y-6">
            {/* Profil Bilgileri */}
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-6">
              <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
                Kişisel Bilgiler
              </h2>
              
              <form onSubmit={handleUpdateProfile} className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Ad
                    </label>
                    <input
                      type="text"
                      value={formData.firstName}
                      onChange={(e) => setFormData({...formData, firstName: e.target.value})}
                      className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white"
                      required
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Soyad
                    </label>
                    <input
                      type="text"
                      value={formData.lastName}
                      onChange={(e) => setFormData({...formData, lastName: e.target.value})}
                      className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white"
                      required
                    />
                  </div>
                </div>
                
              <div>
  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
    Email
  </label>
  <input
    type="email"
    value={formData.email}
    className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white bg-gray-100 dark:bg-gray-600 cursor-not-allowed"
    disabled
  />
  <p className="text-xs text-gray-400 mt-1">Email adresi değiştirilemez</p>
</div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Telefon
                  </label>
                  <input
                    type="tel"
                    value={formData.phone}
                    onChange={(e) => setFormData({...formData, phone: e.target.value})}
                    className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white"
                  />
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Adres
                  </label>
                  <textarea
                    value={formData.address}
                    onChange={(e) => setFormData({...formData, address: e.target.value})}
                    className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white"
                    rows="2"
                  />
                </div>
                
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Şehir
                    </label>
                    <input
                      type="text"
                      value={formData.city}
                      onChange={(e) => setFormData({...formData, city: e.target.value})}
                      className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white"
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      İlçe
                    </label>
                    <input
                      type="text"
                      value={formData.district}
                      onChange={(e) => setFormData({...formData, district: e.target.value})}
                      className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white"
                    />
                  </div>
                </div>
                
                <div className="flex justify-end">
                  <button
                    type="submit"
                    disabled={loading}
                    className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition disabled:opacity-50"
                  >
                    {loading ? 'Kaydediliyor...' : 'Bilgileri Güncelle'}
                  </button>
                </div>
              </form>
            </div>

            {/* Şifre Değiştirme */}
            <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-6">
              <div className="flex justify-between items-center mb-4">
                <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
                  Şifre Değiştir
                </h2>
                <button
                  onClick={() => setShowChangePassword(!showChangePassword)}
                  className="text-blue-600 hover:text-blue-700 text-sm"
                >
                  {showChangePassword ? 'Gizle' : 'Şifre Değiştir'}
                </button>
              </div>

              {showChangePassword && (
                <form onSubmit={handleChangePassword} className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Mevcut Şifre
                    </label>
                    <input
                      type="password"
                      value={currentPassword}
                      onChange={(e) => setCurrentPassword(e.target.value)}
                      className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white"
                      placeholder="Mevcut şifrenizi girin"
                      required
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Yeni Şifre
                    </label>
                    <input
                      type="password"
                      value={newPassword}
                      onChange={(e) => setNewPassword(e.target.value)}
                      className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white"
                      placeholder="En az 6 karakter"
                      required
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Yeni Şifre Tekrar
                    </label>
                    <input
                      type="password"
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                      className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-lg dark:bg-gray-700 dark:text-white"
                      placeholder="Şifreyi tekrar girin"
                      required
                    />
                  </div>
                  
                  <div className="flex justify-end">
                    <button
                      type="submit"
                      disabled={loading}
                      className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg transition disabled:opacity-50"
                    >
                      {loading ? 'Değiştiriliyor...' : 'Şifreyi Değiştir'}
                    </button>
                  </div>
                </form>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}