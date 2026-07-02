import { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import { Link } from 'react-router-dom';
import signalRService from '../services/signalRService';

export default function Customers() {
  const { user } = useAuth();
  const [customers, setCustomers] = useState([]);
  const [personels, setPersonels] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState(null);
  const [activeTab, setActiveTab] = useState('basic');
  
  // ===== EXCEL STATE'LERİ =====
  const [uploading, setUploading] = useState(false);
  const [uploadResult, setUploadResult] = useState(null);
  const [uploadProgress, setUploadProgress] = useState({
    current: 0,
    total: 0,
    email: '',
    accountNumber: '',
    status: 'idle',
    percentage: 0
  });
  
  // 📝 Form State - TÜM ALANLAR
  const [formData, setFormData] = useState({
    // Temel Bilgiler
    accountNumber: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    customerType: 'Bireysel',
    status: 'Pending',
    assignedToPersonelId: '',
    
    // Kurumsal Bilgiler
    companyName: '',
    taxNumber: '',
    taxOffice: '',
    taxAdministration: '',
    website: '',
    contactPerson: '',
    contactPersonPhone: '',
    
    // Finans Bilgileri
    paymentType: '',
    creditLimit: '',
    paymentTermDays: '',
    discountRate: '',
    
    // Adres Bilgileri
    address: '',
    city: '',
    district: '',
    postalCode: '',
    shippingAddress: '',
    invoiceAddress: '',
    
    // Diğer
    notes: ''
  });

  // Filtre State'leri
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [assignedToPersonelId, setAssignedToPersonelId] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [paymentTypeFilter, setPaymentTypeFilter] = useState('');
  const [customerTypeFilter, setCustomerTypeFilter] = useState('');
  const pageSize = 10;

  const hasPermission = (permission) => user?.role === 'SystemAdmin' || user?.role === 'Admin';
  const isCorporate = formData.customerType === 'Kurumsal';

  // Sabit Listeler
  const customerTypes = [
    { value: 'Bireysel', label: '👤 Bireysel' },
    { value: 'Kurumsal', label: '🏢 Kurumsal' },
    { value: 'Potansiyel', label: '🎯 Potansiyel' }
  ];
  
  const statusOptions = [
    { value: 'Active', label: '✅ Aktif' },
    { value: 'Passive', label: '❌ Pasif' },
    { value: 'Pending', label: '⏳ Beklemede' },
    { value: 'Lead', label: '🎯 Potansiyel' },
    { value: 'Lost', label: '💔 Kaybedilen' }
  ];
  
  const paymentTypeOptions = [
    { value: '', label: 'Seçiniz' },
    { value: 'Cash', label: '💵 Peşin' },
    { value: 'Credit', label: '💳 Kredili' },
    { value: 'Deferred', label: '📅 Vadeli' }
  ];

  useEffect(() => {
    fetchCustomers();
    fetchPersonels();
  }, [page, search, assignedToPersonelId, statusFilter, paymentTypeFilter, customerTypeFilter]);

  // ===== PROGRESS EVENT DİNLEYİCİSİ =====
  useEffect(() => {
    console.log('🔵 Customer Progress dinleyici bağlandı');

    const handleProgress = (event) => {
      const data = event.detail;
      console.log('📊 Customer Progress Data:', data);
      
      if (data) {
        setUploadProgress({
          current: data.currentRow || 0,
          total: data.totalRows || 0,
          email: data.currentEmail || 'İşleniyor...',
          accountNumber: data.currentAccountNumber || '',
          status: data.status || 'processing',
          percentage: data.percentage || 0
        });

        if (data.status === 'Completed') {
          console.log('✅ Customer yükleme tamamlandı!');
          setTimeout(() => {
            setUploadProgress({ 
              current: 0, 
              total: 0, 
              email: '', 
              accountNumber: '',
              status: 'idle', 
              percentage: 0 
            });
            setUploading(false);
          }, 1500);
        }

        if (data.status === 'Error') {
          console.log('❌ Customer yükleme hatası:', data.currentEmail);
          setTimeout(() => {
            setUploadProgress(prev => ({ ...prev, status: 'idle' }));
            setUploading(false);
          }, 3000);
        }
      }
    };

    window.addEventListener('uploadProgress', handleProgress);
    return () => window.removeEventListener('uploadProgress', handleProgress);
  }, []);

  // ===== EXCEL SONUÇ MODALI =====
  useEffect(() => {
    if (uploadResult) {
      showUploadResult();
    }
  }, [uploadResult]);

  // 📊 Müşterileri Getir
  const fetchCustomers = async () => {
    try {
      setLoading(true);
      const response = await api.get('/Customers', {
        params: { 
          page, 
          pageSize, 
          search, 
          assignedToPersonelId, 
          status: statusFilter,
          paymentType: paymentTypeFilter,
          customerType: customerTypeFilter
        }
      });
      setCustomers(response.data.data);
      setTotalPages(response.data.totalPages);
      setTotalCount(response.data.totalCount);
    } catch (error) {
      toast.error('Müşteriler yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  // 👥 Personelleri Getir
  const fetchPersonels = async () => {
    try {
      const response = await api.get('/Personels');
      setPersonels(response.data.data || response.data);
    } catch (error) {
      console.error('Personeller yüklenemedi:', error);
    }
  };

  // 🔍 Filtre İşlemleri
  const handleSearch = () => {
    setSearch(searchInput);
    setPage(1);
  };

  const handleClearFilters = () => {
    setSearchInput('');
    setSearch('');
    setAssignedToPersonelId('');
    setStatusFilter('');
    setPaymentTypeFilter('');
    setCustomerTypeFilter('');
    setPage(1);
  };

  // 🔥 Müşteri Aktif/Pasif İşlemleri
  const handleActivate = async (id) => {
    const result = await Swal.fire({
      title: 'Müşteriyi Aktif Yap',
      text: 'Bu müşteriyi aktif hale getirmek istediğinize emin misiniz?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Aktif Yap',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        await api.post(`/Customers/${id}/activate`);
        toast.success('Müşteri aktif hale getirildi');
        fetchCustomers();
      } catch (error) {
        toast.error(error.response?.data?.message || 'İşlem başarısız');
      }
    }
  };

  const handleDeactivate = async (id) => {
    const result = await Swal.fire({
      title: 'Müşteriyi Pasif Yap',
      text: 'Bu müşteriyi pasif hale getirmek istediğinize emin misiniz?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Pasif Yap',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        await api.post(`/Customers/${id}/deactivate`);
        toast.success('Müşteri pasif hale getirildi');
        fetchCustomers();
      } catch (error) {
        toast.error(error.response?.data?.message || 'İşlem başarısız');
      }
    }
  };

  // 💾 Form Submit
  const handleSubmit = async (e) => {
    e.preventDefault();
  
    const submitData = {
      accountNumber: formData.accountNumber,
      firstName: formData.firstName,
      lastName: formData.lastName,
      email: formData.email,
      phone: formData.phone || "",
      customerType: formData.customerType,
      status: formData.status,
      assignedToPersonelId: formData.assignedToPersonelId ? parseInt(formData.assignedToPersonelId) : null,
      companyName: isCorporate ? formData.companyName : null,
      taxNumber: isCorporate ? formData.taxNumber : null,
      taxOffice: isCorporate ? formData.taxOffice : null,
      taxAdministration: formData.taxAdministration || null,
      website: formData.website || null,
      contactPerson: formData.contactPerson || null,
      contactPersonPhone: formData.contactPersonPhone || null,
      paymentType: formData.paymentType || null,
      creditLimit: formData.creditLimit ? parseFloat(formData.creditLimit) : null,
      paymentTermDays: formData.paymentTermDays ? parseInt(formData.paymentTermDays) : null,
      discountRate: formData.discountRate ? parseFloat(formData.discountRate) : null,
      address: formData.address || null,
      city: formData.city || null,
      district: formData.district || null,
      postalCode: formData.postalCode || null,
      shippingAddress: formData.shippingAddress || null,
      invoiceAddress: formData.invoiceAddress || null,
      notes: formData.notes || null
    };
    
    try {
      if (editingCustomer) {
        await api.put(`/Customers/${editingCustomer.id}`, { id: editingCustomer.id, ...submitData });
        toast.success('✅ Müşteri başarıyla güncellendi');
      } else {
        await api.post('/Customers', submitData);
        toast.success('✅ Müşteri başarıyla eklendi');
      }
      
      setShowModal(false);
      setEditingCustomer(null);
      setActiveTab('basic');
      resetForm();
      fetchCustomers();
      
    } catch (error) {
      if (error.response?.data?.errors) {
        const errors = error.response.data.errors;
        const firstError = Object.values(errors)[0]?.[0];
        toast.error(`❌ ${firstError}`);
      } else if (error.response?.data?.message) {
        toast.error(`❌ ${error.response.data.message}`);
      } else {
        toast.error('❌ Bir hata oluştu');
      }
    }
  };

  // 🔄 Form Reset
  const resetForm = () => {
    setFormData({
      accountNumber: '', firstName: '', lastName: '', email: '', phone: '',
      customerType: 'Bireysel', status: 'Pending', assignedToPersonelId: '',
      companyName: '', taxNumber: '', taxOffice: '', taxAdministration: '',
      website: '', contactPerson: '', contactPersonPhone: '',
      paymentType: '', creditLimit: '', paymentTermDays: '', discountRate: '',
      address: '', city: '', district: '', postalCode: '',
      shippingAddress: '', invoiceAddress: '', notes: ''
    });
  };

  // 🗑️ Silme İşlemi
  const handleDelete = async (id, name) => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${name} müşterisini silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal'
    });
    if (result.isConfirmed) {
      try {
        await api.delete(`/Customers/${id}`);
        toast.success('Müşteri başarıyla silindi');
        fetchCustomers();
      } catch (error) {
        toast.error(error.response?.data?.message || 'Silme hatası');
      }
    }
  };

  // ✏️ Düzenleme İşlemi
  const handleEdit = (customer) => {
    setEditingCustomer(customer);
    setFormData({
      accountNumber: customer.accountNumber || '',
      firstName: customer.firstName,
      lastName: customer.lastName,
      email: customer.email,
      phone: customer.phone || '',
      customerType: customer.customerType || 'Bireysel',
      status: customer.status || 'Pending',
      assignedToPersonelId: customer.assignedToPersonelId || '',
      companyName: customer.companyName || '',
      taxNumber: customer.taxNumber || '',
      taxOffice: customer.taxOffice || '',
      taxAdministration: customer.taxAdministration || '',
      website: customer.website || '',
      contactPerson: customer.contactPerson || '',
      contactPersonPhone: customer.contactPersonPhone || '',
      paymentType: customer.paymentType || '',
      creditLimit: customer.creditLimit || '',
      paymentTermDays: customer.paymentTermDays || '',
      discountRate: customer.discountRate || '',
      address: customer.address || '',
      city: customer.city || '',
      district: customer.district || '',
      postalCode: customer.postalCode || '',
      shippingAddress: customer.shippingAddress || '',
      invoiceAddress: customer.invoiceAddress || '',
      notes: customer.notes || ''
    });
    setShowModal(true);
  };

  // ========== EXCEL FONKSİYONLARI ==========

  // Şablon İndir
  const downloadTemplate = async () => {
    try {
      const response = await api.get('/Customers/download-template', {
        responseType: 'blob'
      });
      
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'Musteri_Toplu_Yukleme_Sablonu.xlsx');
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

  // Excel Yükle
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

  // ========== 🔥 SIGNALR GRUBA KATIL ==========
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
    accountNumber: '',
    status: 'processing',
    percentage: 0
  });

  try {
    const response = await api.post('/Customers/upload-excel', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      },
      params: { uploadId: uploadId }
    });

    console.log('✅ Customer Excel yükleme başarılı:', response.data);

    const data = response.data;
    
    if (data.result) {
      const result = data.result;
      setUploadResult(result);

      if (result.errorCount > 0) {
        toast.error(`❌ ${result.errorCount} satır hatalı! Detaylar için modalı inceleyin.`);
      } else {
        toast.success(`✅ ${result.successCount} müşteri başarıyla eklendi!`);
      }
      
      // Modal'ı kapat
      setTimeout(() => {
        setUploadProgress({ 
          current: 0, 
          total: 0, 
          email: '', 
          accountNumber: '',
          status: 'idle', 
          percentage: 0 
        });
        setUploading(false);
      }, 1000);
      
      fetchCustomers();
    } else {
      toast.success(data.message || 'İşlem başarılı!');
      setUploading(false);
      setUploadProgress({ 
        current: 0, 
        total: 0, 
        email: '', 
        accountNumber: '',
        status: 'idle', 
        percentage: 0 
      });
    }

  } catch (error) {
    console.log('=== CUSTOMER EXCEL HATA DETAYI ===');
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
    
    setUploadProgress({ 
      current: 0, 
      total: 0, 
      email: '', 
      accountNumber: '',
      status: 'idle', 
      percentage: 0 
    });
    setUploading(false);
  } finally {
    e.target.value = '';
  }
};

   


  // Sonuç Modalı
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
              <strong>Satır ${err.rowNumber}:</strong> 
              ${err.accountNumber ? `Cari No: ${err.accountNumber} | ` : ''}
              ${err.email || 'Email yok'} 
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

  // 🏷️ Badge Render
  const getStatusBadge = (status) => {
    const config = {
      'Active': 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
      'Passive': 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
      'Pending': 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
      'Lead': 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
      'Lost': 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
    };
    const labels = { 
      'Active': '✅ Aktif', 
      'Passive': '❌ Pasif', 
      'Pending': '⏳ Beklemede', 
      'Lead': '🎯 Potansiyel', 
      'Lost': '💔 Kaybedilen' 
    };
    return <span className={`px-2 py-1 rounded-full text-xs ${config[status] || config['Pending']}`}>{labels[status] || status}</span>;
  };

  const getPaymentTypeBadge = (type) => {
    if (!type) return <span className="text-gray-400">-</span>;
    const config = {
      'Cash': 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-200',
      'Credit': 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200',
      'Deferred': 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200'
    };
    const labels = { 'Cash': '💵 Peşin', 'Credit': '💳 Kredili', 'Deferred': '📅 Vadeli' };
    return <span className={`px-2 py-1 rounded-full text-xs ${config[type]}`}>{labels[type]}</span>;
  };

  // 🏷️ Aktif/Pasif Badge
  const getActiveBadge = (isActive) => {
    return isActive ? (
      <span className="px-2 py-1 rounded-full text-xs bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">
        ✅ Aktif
      </span>
    ) : (
      <span className="px-2 py-1 rounded-full text-xs bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200">
        ❌ Pasif
      </span>
    );
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
               isCompleted ? '✅ Müşteri Yükleme Tamamlandı!' : 
               '📤 Müşteriler Yükleniyor'}
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
                  {uploadProgress.accountNumber && (
                    <span className="block text-xs text-gray-400">Cari No: {uploadProgress.accountNumber}</span>
                  )}
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
                    ✅ {uploadProgress.total} müşteri başarıyla eklendi!
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
                      accountNumber: '',
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

  if (loading) return (
    <div className="flex items-center justify-center h-64">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
    </div>
  );

  return (
    <div className="container mx-auto px-4 py-8">
      {/* ===== PROGRESS MODAL ===== */}
      <ProgressModal />

      {/* ========== HEADER ========== */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Müşteriler</h1>
          <p className="text-sm text-gray-500">Toplam {totalCount} müşteri kaydı</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {/* ===== EXCEL BUTONLARI ===== */}
          {hasPermission('customer.create') && (
            <button
              onClick={downloadTemplate}
              className="bg-emerald-600 hover:bg-emerald-700 text-white px-4 py-2 rounded-lg transition-colors flex items-center gap-2 shadow-sm"
            >
              📥 Şablon İndir
            </button>
          )}
          
          {hasPermission('customer.create') && (
            <label className={`bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg transition-colors cursor-pointer flex items-center gap-2 shadow-sm ${uploading ? 'opacity-50 cursor-not-allowed' : ''}`}>
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

          <button
            onClick={() => { setEditingCustomer(null); resetForm(); setShowModal(true); }}
            className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg transition-colors flex items-center gap-2 shadow-sm"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Yeni Müşteri
          </button>
        </div>
      </div>

      {/* ========== FİLTRELER ========== */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border p-4 mb-6">
        <div className="flex flex-wrap gap-3">
          <input
            type="text"
            placeholder="Cari No, Ad, Soyad, Email, Şirket ara..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            className="flex-1 min-w-[200px] p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600"
          />
          <select value={customerTypeFilter} onChange={(e) => setCustomerTypeFilter(e.target.value)} className="p-2 border rounded-lg dark:bg-gray-700">
            <option value="">Tüm Tipler</option>
            {customerTypes.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
          </select>
          <select value={paymentTypeFilter} onChange={(e) => setPaymentTypeFilter(e.target.value)} className="p-2 border rounded-lg dark:bg-gray-700">
            <option value="">Tüm Ödemeler</option>
            {paymentTypeOptions.filter(opt => opt.value).map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
          </select>
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className="p-2 border rounded-lg dark:bg-gray-700">
            <option value="">Tüm Durumlar</option>
            {statusOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
          </select>
          <select value={assignedToPersonelId} onChange={(e) => setAssignedToPersonelId(e.target.value)} className="p-2 border rounded-lg dark:bg-gray-700">
            <option value="">Tüm Personeller</option>
            {personels.map(p => <option key={p.id} value={p.id}>{p.firstName} {p.lastName}</option>)}
          </select>
          <button onClick={handleSearch} className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700">Ara</button>
          <button onClick={handleClearFilters} className="bg-gray-500 text-white px-4 py-2 rounded-lg hover:bg-gray-600">Temizle</button>
        </div>
      </div>

      {/* ========== MÜŞTERİ TABLOSU ========== */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-700">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Cari No</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Ad Soyad</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Şirket</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Tip</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Ödeme</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Durum</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Aktif/Pasif</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Sorumlu</th>
                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">İşlemler</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {customers.length === 0 ? (
                <tr>
                  <td colSpan={10} className="px-4 py-12 text-center text-gray-500">
                    Müşteri bulunmuyor
                  </td>
                </tr>
              ) : (
                customers.map((customer) => (
                  <tr key={customer.id} className="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                    <td className="px-4 py-3 font-mono text-sm font-medium text-blue-600 dark:text-blue-400">
                      {customer.accountNumber || '-'}
                    </td>
                    <td className="px-4 py-3 text-gray-800 dark:text-white font-medium">
                      {customer.firstName} {customer.lastName}
                    </td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-300 text-sm">{customer.email}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-300 text-sm">{customer.companyName || '-'}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-300 text-sm">{customer.customerType || '-'}</td>
                    <td className="px-4 py-3">{getPaymentTypeBadge(customer.paymentType)}</td>
                    <td className="px-4 py-3">{getStatusBadge(customer.status)}</td>
                    <td className="px-4 py-3">{getActiveBadge(customer.isActive)}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-300 text-sm">{customer.assignedToPersonelName || '-'}</td>
                    <td className="px-4 py-3 text-center">
                      <div className="flex items-center justify-center gap-2 flex-wrap">
                        <Link 
                          to={`/customer-detail/${customer.id}`}
                          className="p-2 rounded-lg bg-emerald-500 hover:bg-emerald-600 text-white transition-all"
                          title="Detay"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                          </svg>
                        </Link>
                        <button 
                          onClick={() => handleEdit(customer)} 
                          className="p-2 rounded-lg bg-blue-500 hover:bg-blue-600 text-white transition-all"
                          title="Düzenle"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                          </svg>
                        </button>
                        {/* 🔥 AKTİF/PASİF BUTONLARI */}
                        {customer.isActive ? (
                          <button
                            onClick={() => handleDeactivate(customer.id)}
                            className="p-2 rounded-lg bg-amber-500 hover:bg-amber-600 text-white transition-all"
                            title="Pasif Yap"
                          >
                            🔒
                          </button>
                        ) : (
                          <button
                            onClick={() => handleActivate(customer.id)}
                            className="p-2 rounded-lg bg-green-500 hover:bg-green-600 text-white transition-all"
                            title="Aktif Yap"
                          >
                            🔓
                          </button>
                        )}
                        <button 
                          onClick={() => handleDelete(customer.id, `${customer.firstName} ${customer.lastName}`)} 
                          className="p-2 rounded-lg bg-rose-500 hover:bg-rose-600 text-white transition-all"
                          title="Sil"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                          </svg>
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

      {/* ========== PAGINATION ========== */}
      {totalPages > 1 && (
        <div className="flex justify-between items-center mt-4">
          <div className="text-sm text-gray-500">Toplam {totalCount} kayıt</div>
          <div className="flex gap-2">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="px-3 py-1 border rounded-lg disabled:opacity-50 hover:bg-gray-100 transition-colors">◀</button>
            <span className="px-3 py-1 text-sm">Sayfa {page} / {totalPages}</span>
            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="px-3 py-1 border rounded-lg disabled:opacity-50 hover:bg-gray-100 transition-colors">▶</button>
          </div>
        </div>
      )}

      {/* ========== MÜŞTERİ MODAL (SEKMELİ FORM) ========== */}
      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-5xl max-h-[90vh] overflow-y-auto">
            <div className="sticky top-0 bg-white dark:bg-gray-800 px-6 py-4 border-b z-10">
              <h2 className="text-xl font-bold text-gray-800 dark:text-white">
                {editingCustomer ? '✏️ Müşteri Düzenle' : '➕ Yeni Müşteri'}
              </h2>
            </div>

            <form onSubmit={handleSubmit}>
              <div className="flex flex-wrap border-b px-6 gap-1">
                <button type="button" onClick={() => setActiveTab('basic')} className={`px-4 py-2 font-medium rounded-t-lg transition-colors ${activeTab === 'basic' ? 'bg-blue-50 text-blue-600 border-b-2 border-blue-600' : 'text-gray-500 hover:text-gray-700'}`}>
                  📝 Temel Bilgiler
                </button>
                <button type="button" onClick={() => setActiveTab('corporate')} className={`px-4 py-2 font-medium rounded-t-lg transition-colors ${activeTab === 'corporate' ? 'bg-blue-50 text-blue-600 border-b-2 border-blue-600' : 'text-gray-500 hover:text-gray-700'}`}>
                  🏢 Kurumsal Bilgiler
                </button>
                <button type="button" onClick={() => setActiveTab('finance')} className={`px-4 py-2 font-medium rounded-t-lg transition-colors ${activeTab === 'finance' ? 'bg-blue-50 text-blue-600 border-b-2 border-blue-600' : 'text-gray-500 hover:text-gray-700'}`}>
                  💰 Finans Bilgileri
                </button>
                <button type="button" onClick={() => setActiveTab('address')} className={`px-4 py-2 font-medium rounded-t-lg transition-colors ${activeTab === 'address' ? 'bg-blue-50 text-blue-600 border-b-2 border-blue-600' : 'text-gray-500 hover:text-gray-700'}`}>
                  📍 Adres Bilgileri
                </button>
                <button type="button" onClick={() => setActiveTab('other')} className={`px-4 py-2 font-medium rounded-t-lg transition-colors ${activeTab === 'other' ? 'bg-blue-50 text-blue-600 border-b-2 border-blue-600' : 'text-gray-500 hover:text-gray-700'}`}>
                  📌 Diğer
                </button>
              </div>

              <div className="p-6">
                {/* TAB 1: TEMEL BİLGİLER */}
                {activeTab === 'basic' && (
                  <div className="space-y-5">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                          Cari Hesap No <span className="text-red-500">*</span>
                        </label>
                        <input type="text" placeholder="CARİ-001" value={formData.accountNumber} onChange={(e) => setFormData({...formData, accountNumber: e.target.value.toUpperCase()})} className="w-full p-2 border rounded-lg font-mono" required />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Müşteri Tipi</label>
                        <select value={formData.customerType} onChange={(e) => setFormData({...formData, customerType: e.target.value})} className="w-full p-2 border rounded-lg">
                          {customerTypes.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                        </select>
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Durum</label>
                        <select value={formData.status} onChange={(e) => setFormData({...formData, status: e.target.value})} className="w-full p-2 border rounded-lg">
                          {statusOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                        </select>
                      </div>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div className="grid grid-cols-2 gap-3">
                        <div>
                          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Ad <span className="text-red-500">*</span></label>
                          <input type="text" value={formData.firstName} onChange={(e) => setFormData({...formData, firstName: e.target.value})} className="w-full p-2 border rounded-lg" required />
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Soyad <span className="text-red-500">*</span></label>
                          <input type="text" value={formData.lastName} onChange={(e) => setFormData({...formData, lastName: e.target.value})} className="w-full p-2 border rounded-lg" required />
                        </div>
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Email <span className="text-red-500">*</span></label>
                        <input type="email" value={formData.email} onChange={(e) => setFormData({...formData, email: e.target.value})} className="w-full p-2 border rounded-lg" required />
                      </div>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Telefon</label>
                        <input type="tel" value={formData.phone} onChange={(e) => setFormData({...formData, phone: e.target.value})} className="w-full p-2 border rounded-lg" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Sorumlu Personel</label>
                        <select value={formData.assignedToPersonelId} onChange={(e) => setFormData({...formData, assignedToPersonelId: e.target.value})} className="w-full p-2 border rounded-lg">
                          <option value="">Seçiniz</option>
                          {personels.map(p => <option key={p.id} value={p.id}>{p.firstName} {p.lastName}</option>)}
                        </select>
                      </div>
                    </div>
                  </div>
                )}

                {/* TAB 2: KURUMSAL BİLGİLER */}
                {activeTab === 'corporate' && (
                  <div className="space-y-5">
                    {!isCorporate && (
                      <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 rounded-lg p-3">
                        <p className="text-sm text-yellow-700">ℹ️ Kurumsal bilgiler sadece "Kurumsal" tipindeki müşteriler için geçerlidir.</p>
                      </div>
                    )}
                    
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                          Şirket Adı {isCorporate && <span className="text-red-500">*</span>}
                        </label>
                        <input type="text" value={formData.companyName} onChange={(e) => setFormData({...formData, companyName: e.target.value})} className="w-full p-2 border rounded-lg" required={isCorporate} />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                          Vergi No {isCorporate && <span className="text-red-500">*</span>}
                        </label>
                        <input type="text" value={formData.taxNumber} onChange={(e) => setFormData({...formData, taxNumber: e.target.value})} className="w-full p-2 border rounded-lg" required={isCorporate} />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                          Vergi Dairesi {isCorporate && <span className="text-red-500">*</span>}
                        </label>
                        <input type="text" value={formData.taxOffice} onChange={(e) => setFormData({...formData, taxOffice: e.target.value})} className="w-full p-2 border rounded-lg" required={isCorporate} />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Vergi İdaresi</label>
                        <input type="text" value={formData.taxAdministration} onChange={(e) => setFormData({...formData, taxAdministration: e.target.value})} className="w-full p-2 border rounded-lg" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Web Sitesi</label>
                        <input type="url" placeholder="https://" value={formData.website} onChange={(e) => setFormData({...formData, website: e.target.value})} className="w-full p-2 border rounded-lg" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İlgili Kişi</label>
                        <input type="text" value={formData.contactPerson} onChange={(e) => setFormData({...formData, contactPerson: e.target.value})} className="w-full p-2 border rounded-lg" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İlgili Kişi Telefon</label>
                        <input type="tel" value={formData.contactPersonPhone} onChange={(e) => setFormData({...formData, contactPersonPhone: e.target.value})} className="w-full p-2 border rounded-lg" />
                      </div>
                    </div>
                  </div>
                )}

                {/* TAB 3: FİNANS BİLGİLERİ */}
                {activeTab === 'finance' && (
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Ödeme Tipi</label>
                      <select value={formData.paymentType} onChange={(e) => setFormData({...formData, paymentType: e.target.value})} className="w-full p-2 border rounded-lg">
                        {paymentTypeOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                      </select>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Vade Gün Sayısı</label>
                      <input type="number" placeholder="30" value={formData.paymentTermDays} onChange={(e) => setFormData({...formData, paymentTermDays: e.target.value})} className="w-full p-2 border rounded-lg" />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Kredi Limiti (₺)</label>
                      <input type="number" step="0.01" placeholder="0.00" value={formData.creditLimit} onChange={(e) => setFormData({...formData, creditLimit: e.target.value})} className="w-full p-2 border rounded-lg" />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İndirim Oranı (%)</label>
                      <input type="number" step="0.01" placeholder="0" value={formData.discountRate} onChange={(e) => setFormData({...formData, discountRate: e.target.value})} className="w-full p-2 border rounded-lg" />
                    </div>
                  </div>
                )}

                {/* TAB 4: ADRES BİLGİLERİ */}
                {activeTab === 'address' && (
                  <div className="space-y-5">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Adres</label>
                      <textarea rows="2" value={formData.address} onChange={(e) => setFormData({...formData, address: e.target.value})} className="w-full p-2 border rounded-lg" />
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Şehir</label>
                        <input type="text" value={formData.city} onChange={(e) => setFormData({...formData, city: e.target.value})} className="w-full p-2 border rounded-lg" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İlçe</label>
                        <input type="text" value={formData.district} onChange={(e) => setFormData({...formData, district: e.target.value})} className="w-full p-2 border rounded-lg" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Posta Kodu</label>
                        <input type="text" value={formData.postalCode} onChange={(e) => setFormData({...formData, postalCode: e.target.value})} className="w-full p-2 border rounded-lg" />
                      </div>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Teslimat Adresi (Farklı ise)</label>
                      <textarea rows="2" placeholder="Farklı teslimat adresi varsa giriniz" value={formData.shippingAddress} onChange={(e) => setFormData({...formData, shippingAddress: e.target.value})} className="w-full p-2 border rounded-lg" />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Fatura Adresi (Farklı ise)</label>
                      <textarea rows="2" placeholder="Farklı fatura adresi varsa giriniz" value={formData.invoiceAddress} onChange={(e) => setFormData({...formData, invoiceAddress: e.target.value})} className="w-full p-2 border rounded-lg" />
                    </div>
                  </div>
                )}

                {/* TAB 5: DİĞER BİLGİLER */}
                {activeTab === 'other' && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Notlar</label>
                    <textarea rows="5" value={formData.notes} onChange={(e) => setFormData({...formData, notes: e.target.value})} className="w-full p-2 border rounded-lg" placeholder="Müşteri hakkında eklemek istedikleriniz..." />
                  </div>
                )}
              </div>

              <div className="sticky bottom-0 bg-white dark:bg-gray-800 px-6 py-4 border-t flex justify-end gap-3">
                <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border rounded-lg hover:bg-gray-100 transition-colors">
                  İptal
                </button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors">
                  {editingCustomer ? 'Güncelle' : 'Kaydet'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}