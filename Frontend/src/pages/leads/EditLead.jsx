// src/pages/leads/EditLead.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import leadApi from '../../services/leadApi';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function EditLead() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);
  const [pageLoading, setPageLoading] = useState(true);
  const [personels, setPersonels] = useState([]);
  const [sourceOptions, setSourceOptions] = useState([]);
  const [statusOptions, setStatusOptions] = useState([]);
  const [campaigns, setCampaigns] = useState([]); 
  
  const [formData, setFormData] = useState({
    companyName: '',
    contactName: '',
    email: '',
    phone: '',
    source: '',
    status: '',
    assignedToPersonelId: '',
    potentialRevenue: '',
    nextFollowUpDate: '',
    notes: '',
    campaignId: '' 
  });

  const currentPersonelId = user?.personelId;

  useEffect(() => {
    fetchLead();
    fetchPersonels();
    fetchLists();
    fetchCampaigns(); 
  }, [id]);

  const fetchLead = async () => {
    try {
      setPageLoading(true);
      const data = await leadApi.getById(id);
      setFormData({
        companyName: data.companyName || '',
        contactName: data.contactName || '',
        email: data.email || '',
        phone: data.phone || '',
        source: data.source || '',
        status: data.status || '',
        assignedToPersonelId: data.assignedToPersonelId || '',
        potentialRevenue: data.potentialRevenue || '',
        nextFollowUpDate: data.nextFollowUpDate?.split('T')[0] || '',
        notes: data.notes || '',
        campaignId: data.campaignId || '' 
      });
    } catch (error) {
      toast.error('Lead bilgileri yüklenemedi');
      navigate('/leads');
    } finally {
      setPageLoading(false);
    }
  };

  const fetchPersonels = async () => {
    try {
      const response = await api.get('/tickets/personel-list');
      if (Array.isArray(response.data)) {
        setPersonels(response.data);
      }
    } catch (error) {
      console.error('Personeller yüklenemedi:', error);
    }
  };

  const fetchLists = async () => {
    try {
      const [sourceRes, statusRes] = await Promise.all([
        leadApi.getSourceList(),
        leadApi.getStatusList()
      ]);
      setSourceOptions(sourceRes);
      setStatusOptions(statusRes);
    } catch (error) {
      console.error('Listeler yüklenemedi:', error);
    }
  };

  // 🔥 YENİ FONKSİYON - Kampanya listesini çek
  const fetchCampaigns = async () => {
    try {
      const response = await api.get('/campaigns');
      const activeCampaigns = response.data.data?.filter(c => c.status === 'Aktif' || c.status === 'Taslak') || [];
      setCampaigns(activeCampaigns);
    } catch (error) {
      console.error('Kampanyalar yüklenemedi:', error);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!formData.companyName.trim()) {
      toast.error('Firma adı zorunludur');
      return;
    }
    if (!formData.contactName.trim()) {
      toast.error('Yetkili kişi zorunludur');
      return;
    }
    if (!formData.email.trim()) {
      toast.error('Email zorunludur');
      return;
    }
    if (!formData.phone.trim()) {
      toast.error('Telefon zorunludur');
      return;
    }

    setLoading(true);
    try {
      const submitData = {
        id: parseInt(id),
        companyName: formData.companyName,
        contactName: formData.contactName,
        email: formData.email,
        phone: formData.phone,
        source: formData.source || null,
        status: formData.status,
        assignedToPersonelId: formData.assignedToPersonelId ? parseInt(formData.assignedToPersonelId) : null,
        potentialRevenue: formData.potentialRevenue ? parseFloat(formData.potentialRevenue) : null,
        nextFollowUpDate: formData.nextFollowUpDate || null,
        notes: formData.notes || null,
        campaignId: formData.campaignId ? parseInt(formData.campaignId) : null // 🔥 EKLE
      };
      await leadApi.update(id, submitData);
      toast.success('✅ Lead başarıyla güncellendi');
      navigate('/leads');
    } catch (error) {
      toast.error(error.response?.data?.message || 'Lead güncellenemedi');
    } finally {
      setLoading(false);
    }
  };

  const inputStyle = "w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200 text-sm";
  const labelStyle = "text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block";

  if (pageLoading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-3xl">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Lead Düzenle</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
            {formData.companyName} leadini düzenleyin
          </p>
        </div>
        <Link to="/leads" className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition">
          <span>←</span> Leadlere Dön
        </Link>
      </div>

      <form onSubmit={handleSubmit} className="space-y-5">
        <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm overflow-hidden">
          <div className="px-5 py-3 bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-2">
              <span className="text-base">🎯</span>
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">
                Lead Bilgileri
              </h2>
            </div>
          </div>
          
          <div className="p-5 space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className={labelStyle}>Firma Adı <span className="text-red-500">*</span></label>
                <input type="text" value={formData.companyName} onChange={(e) => setFormData({...formData, companyName: e.target.value})} className={inputStyle} required />
              </div>
              <div>
                <label className={labelStyle}>Yetkili Kişi <span className="text-red-500">*</span></label>
                <input type="text" value={formData.contactName} onChange={(e) => setFormData({...formData, contactName: e.target.value})} className={inputStyle} required />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className={labelStyle}>Email <span className="text-red-500">*</span></label>
                <input type="email" value={formData.email} onChange={(e) => setFormData({...formData, email: e.target.value})} className={inputStyle} required />
              </div>
              <div>
                <label className={labelStyle}>Telefon <span className="text-red-500">*</span></label>
                <input type="tel" value={formData.phone} onChange={(e) => setFormData({...formData, phone: e.target.value})} className={inputStyle} required />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className={labelStyle}>Kaynak</label>
                <select value={formData.source} onChange={(e) => setFormData({...formData, source: e.target.value})} className={inputStyle}>
                  <option value="">Seçiniz</option>
                  {sourceOptions.map(opt => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelStyle}>Durum</label>
                <select value={formData.status} onChange={(e) => setFormData({...formData, status: e.target.value})} className={inputStyle}>
                  {statusOptions.map(opt => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className={labelStyle}>Atanan Personel</label>
                <select value={formData.assignedToPersonelId} onChange={(e) => setFormData({...formData, assignedToPersonelId: e.target.value})} className={inputStyle}>
                  <option value="">Seçiniz</option>
                  {personels.map(p => (
                    <option key={p.id} value={p.id}>{p.firstName} {p.lastName} {p.id === currentPersonelId ? '(Ben)' : ''}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelStyle}>Potansiyel Gelir (₺)</label>
                <input type="number" step="0.01" value={formData.potentialRevenue} onChange={(e) => setFormData({...formData, potentialRevenue: e.target.value})} className={inputStyle} />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className={labelStyle}>Sonraki Takip Tarihi</label>
                <input type="date" value={formData.nextFollowUpDate} onChange={(e) => setFormData({...formData, nextFollowUpDate: e.target.value})} className={inputStyle} />
              </div>
              <div>
                {/*  KAMPANYA SEÇİMİ */}
                <label className={labelStyle}>Kampanya</label>
                <select value={formData.campaignId} onChange={(e) => setFormData({...formData, campaignId: e.target.value})} className={inputStyle}>
                  <option value="">Seçiniz</option>
                  {campaigns.map(c => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <label className={labelStyle}>Notlar</label>
              <textarea rows="3" value={formData.notes} onChange={(e) => setFormData({...formData, notes: e.target.value})} className={`${inputStyle} resize-none`} placeholder="Lead hakkında notlar..." />
            </div>
          </div>
        </div>

        <div className="flex justify-end gap-3 pt-2">
          <Link to="/leads" className="px-4 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-200 font-medium rounded-lg text-sm hover:bg-gray-100 dark:hover:bg-gray-700 transition">İptal</Link>
          <button type="submit" disabled={loading} className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 dark:disabled:bg-indigo-800 text-white font-semibold rounded-lg text-sm transition disabled:cursor-not-allowed flex items-center gap-1">
            {loading ? 'Güncelleniyor...' : 'Değişiklikleri Kaydet'}
          </button>
        </div>
      </form>
    </div>
  );
}