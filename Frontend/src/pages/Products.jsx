import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';
import api from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import { getImageUrl } from '../helpers/imageHelper'; 
import signalRService from '../services/signalRService';

export default function Products() {
  const { user } = useAuth();
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [brands, setBrands] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [selectedFile, setSelectedFile] = useState(null);
  
  // Filtreler
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [brandId, setBrandId] = useState('');
  const [isActive, setIsActive] = useState('');
  const pageSize = 10;

  const [formData, setFormData] = useState({
    name: '',
    sku: '',
    barcode: '',
    description: '',
    price: '',
    currency: 'TRY',
    stockQuantity: '',
    minStockLevel: '',
    maxStockLevel: '',
    categoryId: '',
    brandId: '',
    unit: 'Adet',
    imageUrl: '',
    isActive: true,
    isStockTrackable: true
  });

  // ===== EXCEL STATE'LERİ =====
  const [excelUploading, setExcelUploading] = useState(false);
  const [uploadResult, setUploadResult] = useState(null);
  const [uploadProgress, setUploadProgress] = useState({
    current: 0,
    total: 0,
    name: '',
    status: 'idle',
    percentage: 0
  });

  const hasPermission = (permission) => {
    return user?.role === 'SystemAdmin' || user?.role === 'Admin';
  };

  // ===== PROGRESS EVENT DİNLEYİCİSİ =====
  useEffect(() => {
    console.log('🔵 Product Progress dinleyici bağlandı');

    const handleProgress = (event) => {
      const data = event.detail;
      console.log('📊 Product Progress Data:', data);
      
      if (data) {
        setUploadProgress({
          current: data.currentRow || 0,
          total: data.totalRows || 0,
          name: data.currentName || 'İşleniyor...',
          status: data.status || 'processing',
          percentage: data.percentage || 0
        });

        if (data.status === 'Completed') {
          console.log('✅ Product yükleme tamamlandı!');
          setTimeout(() => {
            setUploadProgress({ 
              current: 0, 
              total: 0, 
              name: '', 
              status: 'idle', 
              percentage: 0 
            });
            setExcelUploading(false);
          }, 1500);
        }

        if (data.status === 'Error') {
          console.log('❌ Product yükleme hatası:', data.currentName);
          setTimeout(() => {
            setUploadProgress(prev => ({ ...prev, status: 'idle' }));
            setExcelUploading(false);
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

  useEffect(() => {
    fetchProducts();
    fetchCategories();
    fetchBrands();
  }, [page, search, categoryId, brandId, isActive]);

  const fetchProducts = async () => {
    try {
      setLoading(true);
      const response = await api.get('/Products', {
        params: { page, pageSize, search, categoryId, brandId, isActive }
      });
      setProducts(response.data.data || []);
      setTotalPages(response.data.totalPages || 1);
      setTotalCount(response.data.totalCount || 0);
    } catch (error) {
      toast.error('Ürünler yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const fetchCategories = async () => {
    try {
      const response = await api.get('/Products/categories-list');
      setCategories(response.data || []);
    } catch (error) {
      console.error('Kategoriler yüklenemedi:', error);
    }
  };

  const fetchBrands = async () => {
    try {
      const response = await api.get('/Brands/select-list');
      setBrands(response.data || []);
    } catch (error) {
      console.error('Markalar yüklenemedi:', error);
    }
  };

  const handleSearch = () => {
    setSearch(searchInput);
    setPage(1);
  };

  const handleClearFilters = () => {
    setSearchInput('');
    setSearch('');
    setCategoryId('');
    setBrandId('');
    setIsActive('');
    setPage(1);
  };

  // ===== EXCEL FONKSİYONLARI =====

  // Şablon İndir
  const downloadTemplate = async () => {
    try {
      const response = await api.get('/Products/download-template', {
        responseType: 'blob'
      });
      
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'Urun_Toplu_Yukleme_Sablonu.xlsx');
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

    // ===== SIGNALR GRUBA KATIL =====
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

    const formData = new FormData();
    formData.append('file', file);

    setExcelUploading(true);
    setUploadResult(null);
    setUploadProgress({
      current: 0,
      total: 0,
      name: 'Başlatılıyor...',
      status: 'processing',
      percentage: 0
    });

    try {
      const response = await api.post('/Products/upload-excel', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        },
        params: { uploadId: uploadId }
      });

      console.log('✅ Product Excel yükleme başarılı:', response.data);

      const data = response.data;
      
      if (data.result) {
        const result = data.result;
        setUploadResult(result);

        if (result.errorCount > 0) {
          toast.error(`❌ ${result.errorCount} satır hatalı! Detaylar için modalı inceleyin.`);
        } else {
          toast.success(`✅ ${result.successCount} ürün başarıyla eklendi!`);
        }
        
        setTimeout(() => {
          setUploadProgress({ 
            current: 0, 
            total: 0, 
            name: '', 
            status: 'idle', 
            percentage: 0 
          });
          setExcelUploading(false);
        }, 1000);
        
        fetchProducts();
      } else {
        toast.success(data.message || 'İşlem başarılı!');
        setExcelUploading(false);
        setUploadProgress({ 
          current: 0, 
          total: 0, 
          name: '', 
          status: 'idle', 
          percentage: 0 
        });
      }

    } catch (error) {
      console.log('=== PRODUCT EXCEL HATA DETAYI ===');
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
        name: '', 
        status: 'idle', 
        percentage: 0 
      });
      setExcelUploading(false);
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
              ${err.name ? `Ürün: ${err.name} | ` : ''}
              ${err.sku ? `SKU: ${err.sku} | ` : ''}
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

  // ===== PROGRESS MODAL =====
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
               isCompleted ? '✅ Ürün Yükleme Tamamlandı!' : 
               '📤 Ürünler Yükleniyor'}
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
                  İşleniyor: <span className="font-medium text-blue-600">{uploadProgress.name || '...'}</span>
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
                    ✅ {uploadProgress.total} ürün başarıyla eklendi!
                  </p>
                )}
                {isError && (
                  <p className="text-sm text-red-600 dark:text-red-400">
                    {uploadProgress.name || 'Bilinmeyen hata oluştu'}
                  </p>
                )}
                <button
                  onClick={() => {
                    setUploadProgress({ 
                      current: 0, 
                      total: 0, 
                      name: '', 
                      status: 'idle', 
                      percentage: 0 
                    });
                    setExcelUploading(false);
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

  // 📤 Resim Yükleme (dosya seçildiğinde)
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
    
    setSelectedFile(file);
    const previewUrl = URL.createObjectURL(file);
    setFormData(prev => ({ ...prev, imageUrl: previewUrl }));
  };

  // 🚀 Resmi backend'e yükle
  const uploadImageToBackend = async (productId) => {
    if (!selectedFile) return null;
    
    setUploading(true);
    const uploadFormData = new FormData();
    uploadFormData.append('file', selectedFile);
    
    try {
      const response = await api.post(`/Products/${productId}/image`, uploadFormData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      const imageUrl = response.data.imageUrl;
      toast.success('Resim yüklendi');
      return imageUrl;
    } catch (error) {
      console.error('Yükleme hatası:', error);
      toast.error(error.response?.data?.message || 'Resim yüklenemedi');
      return null;
    } finally {
      setUploading(false);
    }
  };

  // 🗑️ Resim Silme
  const handleDeleteImage = async () => {
    if (!editingProduct && !selectedFile) {
      setFormData(prev => ({ ...prev, imageUrl: '' }));
      setSelectedFile(null);
      toast.success('Resim temizlendi');
      return;
    }
    
    if (!editingProduct && selectedFile) {
      setSelectedFile(null);
      setFormData(prev => ({ ...prev, imageUrl: '' }));
      toast.success('Resim temizlendi');
      return;
    }
    
    const result = await Swal.fire({
      title: 'Resmi sil?',
      text: 'Ürün resmini silmek istediğinize emin misiniz?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil',
      cancelButtonText: 'İptal'
    });
    
    if (result.isConfirmed) {
      try {
        await api.delete(`/Products/${editingProduct.id}/image`);
        setFormData(prev => ({ ...prev, imageUrl: '' }));
        setSelectedFile(null);
        toast.success('Resim silindi');
        fetchProducts();
      } catch (error) {
        toast.error('Resim silinemedi');
      }
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    const submitData = {
      id: editingProduct ? editingProduct.id : 0,
      name: formData.name,
      sku: formData.sku || null,
      barcode: formData.barcode || null,
      description: formData.description || null,
      price: parseFloat(formData.price),
      currency: formData.currency,
      stockQuantity: parseInt(formData.stockQuantity),
      minStockLevel: formData.minStockLevel ? parseInt(formData.minStockLevel) : null,
      maxStockLevel: formData.maxStockLevel ? parseInt(formData.maxStockLevel) : null,
      categoryId: formData.categoryId ? parseInt(formData.categoryId) : null,
      brandId: formData.brandId ? parseInt(formData.brandId) : null,
      unit: formData.unit,
      imageUrl: formData.imageUrl?.startsWith('blob:') ? '' : formData.imageUrl,
      isActive: formData.isActive,
      isStockTrackable: formData.isStockTrackable
    };
    
    try {
      if (editingProduct) {
        await api.put(`/Products/${editingProduct.id}`, submitData);
        
        if (selectedFile) {
          await uploadImageToBackend(editingProduct.id);
        }
        
        toast.success('✅ Ürün başarıyla güncellendi');
      } else {
        const response = await api.post('/Products', submitData);
        const newProduct = response.data;
        
        if (selectedFile) {
          const imageUrl = await uploadImageToBackend(newProduct.id);
          if (imageUrl) {
            await api.put(`/Products/${newProduct.id}`, {
              ...submitData,
              id: newProduct.id,
              imageUrl: imageUrl
            });
          }
        }
        
        toast.success('✅ Ürün başarıyla eklendi');
      }
      setShowModal(false);
      setEditingProduct(null);
      setSelectedFile(null);
      resetForm();
      fetchProducts();
    } catch (error) {
      if (error.response?.data?.errors) {
        const errors = error.response.data.errors;
        const firstField = Object.keys(errors)[0];
        const firstMessage = errors[firstField][0];
        toast.error(`❌ ${firstMessage}`);
      } else if (error.response?.data?.message) {
        toast.error(`❌ ${error.response.data.message}`);
      } else {
        toast.error('❌ Bir hata oluştu');
      }
    }
  };

  const resetForm = () => {
    setFormData({
      name: '', sku: '', barcode: '', description: '', price: '', currency: 'TRY',
      stockQuantity: '', minStockLevel: '', maxStockLevel: '', categoryId: '',
      brandId: '', unit: 'Adet', imageUrl: '', isActive: true, isStockTrackable: true
    });
  };

  const handleDelete = async (id, name) => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${name} ürününü silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        const response = await api.delete(`/Products/${id}`);
        if (response.data.isSoftDeleted) {
          toast.warning('Ürün siparişlerde kullanıldığı için pasif hale getirildi');
        } else {
          toast.success('Ürün başarıyla silindi');
        }
        fetchProducts();
      } catch (error) {
        toast.error(error.response?.data?.message || 'Silme hatası');
      }
    }
  };

  const handleEdit = (product) => {
    setEditingProduct(product);
    setSelectedFile(null);
    setFormData({
      name: product.name,
      sku: product.sku || '',
      barcode: product.barcode || '',
      description: product.description || '',
      price: product.price,
      currency: product.currency || 'TRY',
      stockQuantity: product.stockQuantity,
      minStockLevel: product.minStockLevel || '',
      maxStockLevel: product.maxStockLevel || '',
      categoryId: product.categoryId || '',
      brandId: product.brandId || '',
      unit: product.unit || 'Adet',
      imageUrl: product.imageUrl || '',
      isActive: product.isActive,
      isStockTrackable: product.isStockTrackable
    });
    setShowModal(true);
  };

  const formatPrice = (price, currency) => {
    if (!price) return '-';
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency || 'TRY',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(price);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 dark:border-blue-400 mx-auto"></div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      {/* ===== PROGRESS MODAL ===== */}
      <ProgressModal />

      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Ürünler</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} ürün</p>
        </div>
        <div className="flex gap-2 flex-wrap">
          {hasPermission('product.create') && (
            <>
              <button
                onClick={downloadTemplate}
                className="bg-emerald-600 hover:bg-emerald-700 text-white px-4 py-2 rounded-lg transition-colors flex items-center gap-2 shadow-sm"
              >
                📥 Şablon İndir
              </button>
              
              <label className={`bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg transition-colors cursor-pointer flex items-center gap-2 shadow-sm ${excelUploading ? 'opacity-50 cursor-not-allowed' : ''}`}>
                {excelUploading ? (
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
                  disabled={excelUploading}
                />
              </label>
            </>
          )}
          
          {hasPermission('product.create') && (
            <button
              onClick={() => {
                setEditingProduct(null);
                setSelectedFile(null);
                resetForm();
                setShowModal(true);
              }}
              className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg transition-colors flex items-center gap-2 shadow-sm"
            >
              + Yeni Ürün
            </button>
          )}
        </div>
      </div>

      {/* Filtreler */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4 mb-6">
        <div className="flex flex-wrap gap-3">
          <input
            type="text"
            placeholder="Ürün adı, SKU, Barkod ara..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            className="flex-1 p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400"
          />
          <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} className="p-2 border rounded-lg dark:bg-gray-700">
            <option value="">Tüm Kategoriler</option>
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <select value={brandId} onChange={(e) => setBrandId(e.target.value)} className="p-2 border rounded-lg dark:bg-gray-700">
            <option value="">Tüm Markalar</option>
            {brands.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>
          <select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="p-2 border rounded-lg dark:bg-gray-700">
            <option value="">Tüm Durumlar</option>
            <option value="true">Aktif</option>
            <option value="false">Pasif</option>
          </select>
          <button onClick={handleSearch} className="bg-blue-600 text-white px-4 py-2 rounded-lg">Ara</button>
          <button onClick={handleClearFilters} className="bg-gray-500 text-white px-4 py-2 rounded-lg">Temizle</button>
        </div>
      </div>

      {/* Tablo */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-700">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Resim</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Ürün Adı</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">SKU</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Kategori</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Marka</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Fiyat</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Stok</th>
                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Durum</th>
                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {products.length === 0 ? (
                <tr>
                  <td colSpan={9} className="px-4 py-12 text-center text-gray-500 dark:text-gray-400">
                    Ürün bulunmuyor
                  </td>
                </tr>
              ) : (
                products.map((product) => (
                  <tr key={product.id} className="border-t border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                    <td className="px-4 py-3">
                      {product.imageUrl ? (
                        <img 
                          src={getImageUrl(product.imageUrl)} 
                          alt={product.name} 
                          className="w-10 h-10 object-cover rounded-lg"
                          onError={(e) => { e.target.src = 'https://placehold.co/40x40?text=Ürün' }}
                        />
                      ) : (
                        <div className="w-10 h-10 bg-gray-100 dark:bg-gray-700 rounded-lg flex items-center justify-center text-gray-400">
                          📦
                        </div>
                      )}
                    </td>
                    <td className="px-4 py-3 font-medium text-gray-800 dark:text-white">{product.name}</td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-300">{product.sku || '-'}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-300">{product.categoryName || '-'}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-300">{product.brandName || '-'}</td>
                    <td className="px-4 py-3 font-semibold text-gray-800 dark:text-white">{formatPrice(product.price, product.currency)}</td>
                    <td className="px-4 py-3">
                      <span className={`${product.stockQuantity < 10 ? 'text-red-600 dark:text-red-400 font-bold' : 'text-gray-700 dark:text-gray-300'}`}>
                        {product.stockQuantity}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      <span className={`px-2 py-1 rounded-full text-xs ${
                        product.isActive 
                          ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' 
                          : 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200'
                      }`}>
                        {product.isActive ? 'Aktif' : 'Pasif'}
                      </span>
                    </td>
                    
                    <td className="px-4 py-3 text-center">
                      <div className="flex justify-center gap-2">
                        <Link 
                          to={`/products/${product.id}`}
                          state={{ from: '/products' }}
                          className="p-1.5 text-emerald-600 hover:bg-emerald-50 dark:text-emerald-400 dark:hover:bg-emerald-900/30 rounded-lg transition-colors"
                          title="Detay"
                        >
                          👁️
                        </Link>
                        
                        {hasPermission('product.edit') && (
                          <button 
                            onClick={() => handleEdit(product)} 
                            className="p-1.5 text-blue-600 hover:bg-blue-50 dark:text-blue-400 dark:hover:bg-blue-900/30 rounded-lg transition-colors"
                            title="Düzenle"
                          >
                            ✏️
                          </button>
                        )}
                        {hasPermission('product.delete') && (
                          <button 
                            onClick={() => handleDelete(product.id, product.name)} 
                            className="p-1.5 text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-900/30 rounded-lg transition-colors"
                            title="Sil"
                          >
                            🗑️
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
          <div className="text-sm text-gray-500">Toplam {totalCount} kayıt</div>
          <div className="flex gap-2">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="px-3 py-1 border rounded disabled:opacity-50">◀</button>
            <span className="px-3 py-1 text-sm">Sayfa {page} / {totalPages}</span>
            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="px-3 py-1 border rounded disabled:opacity-50">▶</button>
          </div>
        </div>
      )}

      {/* Ürün Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
            <div className="sticky top-0 bg-white dark:bg-gray-800 px-6 py-4 border-b">
              <h2 className="text-xl font-bold">{editingProduct ? 'Ürün Düzenle' : 'Yeni Ürün'}</h2>
            </div>
            <form onSubmit={handleSubmit} className="p-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-3">
                  <input type="text" placeholder="Ürün Adı *" value={formData.name} onChange={(e) => setFormData({...formData, name: e.target.value})} className="w-full p-2 border rounded-lg" required />
                  <input type="text" placeholder="SKU" value={formData.sku} onChange={(e) => setFormData({...formData, sku: e.target.value})} className="w-full p-2 border rounded-lg" />
                  <input type="text" placeholder="Barkod" value={formData.barcode} onChange={(e) => setFormData({...formData, barcode: e.target.value})} className="w-full p-2 border rounded-lg" />
                  <textarea placeholder="Açıklama" value={formData.description} onChange={(e) => setFormData({...formData, description: e.target.value})} className="w-full p-2 border rounded-lg" rows="2" />
                </div>
                
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <input type="number" step="0.01" placeholder="Fiyat *" value={formData.price} onChange={(e) => setFormData({...formData, price: e.target.value})} className="w-full p-2 border rounded-lg" required />
                    <select value={formData.currency} onChange={(e) => setFormData({...formData, currency: e.target.value})} className="w-full p-2 border rounded-lg">
                      <option value="TRY">₺ TL</option>
                      <option value="USD">$ USD</option>
                      <option value="EUR">€ EUR</option>
                      <option value="GBP">£ GBP</option>
                    </select>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <input type="number" placeholder="Stok Miktarı *" value={formData.stockQuantity} onChange={(e) => setFormData({...formData, stockQuantity: e.target.value})} className="w-full p-2 border rounded-lg" required />
                    <select value={formData.unit} onChange={(e) => setFormData({...formData, unit: e.target.value})} className="w-full p-2 border rounded-lg">
                      <option>Adet</option><option>Kg</option><option>Litre</option><option>Metre</option>
                    </select>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <input type="number" placeholder="Min. Stok" value={formData.minStockLevel} onChange={(e) => setFormData({...formData, minStockLevel: e.target.value})} className="w-full p-2 border rounded-lg" />
                    <input type="number" placeholder="Max. Stok" value={formData.maxStockLevel} onChange={(e) => setFormData({...formData, maxStockLevel: e.target.value})} className="w-full p-2 border rounded-lg" />
                  </div>
                  <select value={formData.categoryId} onChange={(e) => setFormData({...formData, categoryId: e.target.value})} className="w-full p-2 border rounded-lg">
                    <option value="">Kategori Seçin</option>
                    {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                  </select>
                  <select value={formData.brandId} onChange={(e) => setFormData({...formData, brandId: e.target.value})} className="w-full p-2 border rounded-lg">
                    <option value="">Marka Seçin</option>
                    {brands.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
                  </select>
                  
                  <div className="mb-2">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Ürün Resmi</label>
                    
                    {formData.imageUrl && (
                      <div className="mb-2 flex items-center gap-3 p-2 bg-gray-50 rounded-lg">
                        <img src={getImageUrl(formData.imageUrl)} alt="Ürün" className="w-12 h-12 object-cover rounded" />
                        <div className="flex-1">
                          <p className="text-xs text-gray-500 truncate">{formData.imageUrl?.startsWith('blob:') ? 'Yeni seçilen resim' : formData.imageUrl}</p>
                          <button type="button" onClick={handleDeleteImage} className="text-xs text-red-500 hover:text-red-600">🗑️ Resmi Kaldır</button>
                        </div>
                      </div>
                    )}
                    
                    <label className={`flex items-center justify-center gap-2 px-4 py-2 border-2 border-dashed rounded-lg cursor-pointer ${uploading ? 'bg-gray-100' : 'border-blue-400 hover:bg-blue-50'}`}>
                      <input type="file" accept="image/*" onChange={(e) => handleFileSelect(e.target.files[0])} className="hidden" disabled={uploading} />
                      {uploading ? <><div className="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div><span>Yükleniyor...</span></> : <>📁 Resim Seç</>}
                    </label>
                    
                    <p className="text-xs text-gray-400 mt-1">Önerilen: Kare format, max 2MB (JPEG, PNG, WEBP)</p>
                    
                    <div className="relative my-2"><div className="absolute inset-0 flex items-center"><div className="w-full border-t border-gray-300"></div></div><div className="relative flex justify-center text-xs"><span className="px-2 bg-white text-gray-500">VEYA</span></div></div>
                    
                    <input type="text" placeholder="https://example.com/resim.jpg" value={formData.imageUrl?.startsWith('blob:') ? '' : formData.imageUrl || ''} onChange={(e) => { setSelectedFile(null); setFormData({...formData, imageUrl: e.target.value}); }} className="w-full p-2 border rounded-lg" />
                  </div>
                  
                  <div className="flex gap-4">
                    <label className="flex items-center gap-2"><input type="checkbox" checked={formData.isActive} onChange={(e) => setFormData({...formData, isActive: e.target.checked})} className="w-4 h-4" /><span className="text-sm">Aktif</span></label>
                    <label className="flex items-center gap-2"><input type="checkbox" checked={formData.isStockTrackable} onChange={(e) => setFormData({...formData, isStockTrackable: e.target.checked})} className="w-4 h-4" /><span className="text-sm">Stok Takibi</span></label>
                  </div>
                </div>
              </div>
              <div className="flex justify-end gap-3 mt-6 pt-4 border-t">
                <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border rounded-lg hover:bg-gray-100">İptal</button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700" disabled={uploading}>{uploading ? 'Yükleniyor...' : (editingProduct ? 'Güncelle' : 'Kaydet')}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}