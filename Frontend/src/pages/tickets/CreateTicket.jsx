// src/pages/tickets/CreateTicket.jsx
import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import ticketApi from '../../services/ticketApi';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function CreateTicket() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);
  const [customers, setCustomers] = useState([]);
  const [personels, setPersonels] = useState([]);  // 🔥 YENİ: Personel listesi
  const [priorityList, setPriorityList] = useState([]);
  const [categoryList, setCategoryList] = useState([]);
  
  const [formData, setFormData] = useState({
    subject: '',
    description: '',
    customerId: '',
    priority: 'Medium',
    category: '',
    assignedToPersonelId: null  // 🔥 Zaten var
  });

  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';

  useEffect(() => {
    fetchCustomers();
    fetchPersonels();  // 🔥 YENİ: Personelleri çek
    fetchLists();
  }, []);

  const fetchCustomers = async () => {
    try {
      const response = await api.get('/tickets/customer-list');
      setCustomers(response.data || []);
    } catch (error) {
      toast.error('Müşteriler yüklenemedi');
    }
  };


// fetchPersonels fonksiyonunu değiştir
const fetchPersonels = async () => {
  try {
    // 🔥 TicketController'daki yeni endpoint'i kullan
    const response = await api.get('/tickets/personel-list');
    console.log('Personel listesi:', response.data);
    
    if (Array.isArray(response.data)) {
      setPersonels(response.data);
    } else {
      setPersonels([]);
    }
  } catch (error) {
    console.error('Personeller yüklenemedi:', error);
    setPersonels([]);
  }
};

  const fetchLists = async () => {
    try {
      const [priority, category] = await Promise.all([
        ticketApi.getPriorityList(),
        ticketApi.getCategoryList()
      ]);
      setPriorityList(priority);
      setCategoryList(category);
    } catch (error) {
      console.error('Listeler yüklenemedi:', error);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!formData.subject.trim()) {
      toast.error('Konu alanı zorunludur');
      return;
    }
    if (!formData.customerId) {
      toast.error('Müşteri seçilmelidir');
      return;
    }

    setLoading(true);
    try {
      const result = await ticketApi.create(formData);
      console.log('Başarılı:', result);
      toast.success('✅ Destek talebi başarıyla oluşturuldu');
      navigate('/tickets');
    } catch (error) {
      console.error('Full error:', error);
      console.error('Response data:', error.response?.data);
      console.error('Response status:', error.response?.status);
      
      if (error.response?.data) {
        if (error.response.data.message) {
          toast.error(error.response.data.message);
        } else if (error.response.data.title) {
          toast.error(error.response.data.title);
        } else if (error.response.data.errors) {
          const errors = Object.values(error.response.data.errors).flat();
          toast.error(errors[0]);
        } else {
          toast.error(JSON.stringify(error.response.data));
        }
      } else {
        toast.error('Talep oluşturulamadı');
      }
    } finally {
      setLoading(false);
    }
  };

  const inputStyle = "w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200 text-sm";
  const labelStyle = "text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block";

  return (
    <div className="container mx-auto px-4 py-8 max-w-3xl">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Yeni Destek Talebi</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
            Müşteri desteği için yeni talep oluşturun
          </p>
        </div>
        <Link
          to="/tickets"
          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition"
        >
          <span>←</span> Taleplere Dön
        </Link>
      </div>

      <form onSubmit={handleSubmit} className="space-y-5">
        <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm overflow-hidden">
          <div className="px-5 py-3 bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-2">
              <span className="text-base">🎫</span>
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">
                Talep Bilgileri
              </h2>
            </div>
          </div>
          
          <div className="p-5 space-y-4">
            {/* Müşteri */}
            <div>
              <label className={labelStyle}>Müşteri <span className="text-red-500">*</span></label>
              <select
                value={formData.customerId}
                onChange={(e) => setFormData({ ...formData, customerId: e.target.value })}
                className={inputStyle}
                required
              >
                <option value="">Müşteri Seçin</option>
                {customers.map(c => (
                  <option key={c.id} value={c.id}>
                    {c.firstName} {c.lastName} {c.companyName ? `(${c.companyName})` : ''}
                  </option>
                ))}
              </select>
            </div>

            {/* Konu */}
            <div>
              <label className={labelStyle}>Konu <span className="text-red-500">*</span></label>
              <input
                type="text"
                value={formData.subject}
                onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
                placeholder="Talep konusunu girin..."
                className={inputStyle}
                required
              />
            </div>

            {/* Açıklama */}
            <div>
              <label className={labelStyle}>Açıklama</label>
              <textarea
                rows="4"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                placeholder="Talep detaylarını girin..."
                className={`${inputStyle} resize-none`}
              />
            </div>

            {/* Priority, Category & Assigned Personel */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className={labelStyle}>Öncelik</label>
                <select
                  value={formData.priority}
                  onChange={(e) => setFormData({ ...formData, priority: e.target.value })}
                  className={inputStyle}
                >
                  {priorityList.map(p => (
                    <option key={p.value} value={p.value}>{p.label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelStyle}>Kategori</label>
                <select
                  value={formData.category}
                  onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                  className={inputStyle}
                >
                  <option value="">Seçiniz</option>
                  {categoryList.map(c => (
                    <option key={c.value} value={c.value}>{c.label}</option>
                  ))}
                </select>
              </div>
              {/* 🔥 YENİ: Atanan Personel */}
              <div>
                <label className={labelStyle}>Atanan Personel</label>
                <select
                  value={formData.assignedToPersonelId || ''}
                  onChange={(e) => setFormData({ ...formData, assignedToPersonelId: e.target.value ? parseInt(e.target.value) : null })}
                  className={inputStyle}
                >
                  <option value="">Atanacak Personel Seçin</option>
                  {personels.map(p => (
                    <option key={p.id} value={p.id}>
                      {p.firstName} {p.lastName} - {p.email}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </div>
        </div>

        {/* Buttons */}
        <div className="flex justify-end gap-3 pt-2">
          <Link
            to="/tickets"
            className="px-4 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-200 font-medium rounded-lg text-sm hover:bg-gray-100 dark:hover:bg-gray-700 transition"
          >
            İptal
          </Link>
          <button
            type="submit"
            disabled={loading}
            className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 dark:disabled:bg-indigo-800 text-white font-semibold rounded-lg text-sm transition disabled:cursor-not-allowed flex items-center gap-1"
          >
            {loading ? 'Oluşturuluyor...' : 'Talep Oluştur'}
          </button>
        </div>
      </form>
    </div>
  );
}