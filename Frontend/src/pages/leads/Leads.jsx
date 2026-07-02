// src/pages/leads/Leads.jsx
import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useSignalR } from '../../contexts/SignalRContext';
import leadApi from '../../services/leadApi';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function Leads() {
  const { user } = useAuth();
  const { isConnected, refreshSignal } = useSignalR();
  const [leads, setLeads] = useState([]);
  const [loading, setLoading] = useState(true);
  const [personels, setPersonels] = useState([]);
  
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 10;
  
  const [filters, setFilters] = useState({
    search: '',
    status: '',
    source: '',
    assignedToPersonelId: ''
  });
  
  const [statusOptions, setStatusOptions] = useState([]);
  const [sourceOptions, setSourceOptions] = useState([]);
  
  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
  const [showConvertModal, setShowConvertModal] = useState(false);
  const [selectedLead, setSelectedLead] = useState(null);
  const [convertForm, setConvertForm] = useState({
    taxNumber: '',
    taxOffice: '',
    address: '',
    city: '',
    district: ''
  });

  useEffect(() => {
    fetchFilters();
    fetchPersonels();
  }, []);

  useEffect(() => {
    fetchLeads();
  }, [page, filters, refreshSignal]);

  const fetchLeads = useCallback(async () => {
    try {
      setLoading(true);
      const params = { page, pageSize, ...filters };
      const response = await leadApi.getAll(params);
      setLeads(response.data || []);
      setTotalPages(response.totalPages || 1);
      setTotalCount(response.totalCount || 0);
    } catch (error) {
      toast.error('Leadler yüklenemedi');
    } finally {
      setLoading(false);
    }
  }, [page, filters]);

  const fetchFilters = async () => {
    try {
      const [statusRes, sourceRes] = await Promise.all([
        leadApi.getStatusList(),
        leadApi.getSourceList()
      ]);
      setStatusOptions(statusRes);
      setSourceOptions(sourceRes);
    } catch (error) {
      console.error('Filtreler yüklenemedi:', error);
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

  const handleFilterChange = (key, value) => {
    setFilters(prev => ({ ...prev, [key]: value }));
    setPage(1);
  };

  const handleClearFilters = () => {
    setFilters({ search: '', status: '', source: '', assignedToPersonelId: '' });
    setPage(1);
  };

  const handleDelete = async (id, companyName) => {
    const result = await Swal.fire({
      title: 'Emin misiniz?',
      text: `${companyName} leadini silmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Evet, sil!',
      cancelButtonText: 'İptal'
    });

    if (result.isConfirmed) {
      try {
        await leadApi.delete(id);
        toast.success('Lead silindi');
        fetchLeads();
      } catch (error) {
        toast.error('Silme başarısız');
      }
    }
  };

  const openConvertModal = (lead) => {
    setSelectedLead(lead);
    setConvertForm({
      taxNumber: '',
      taxOffice: '',
      address: '',
      city: '',
      district: ''
    });
    setShowConvertModal(true);
  };

  const handleConvert = async () => {
    try {
      console.log('Gönderilen veri:', convertForm);
      const response = await leadApi.convertToCustomer(selectedLead.id, convertForm);
      console.log('Başarılı:', response);
      toast.success('Lead başarıyla müşteriye dönüştürüldü');
      setShowConvertModal(false);
      fetchLeads();
    } catch (error) {
      console.error('Hata detayı:', error.response?.data); 
      console.error('Status:', error.response?.status);
      console.error('Hata mesajı:', error.response?.data?.message);
      console.error('Validation errors:', error.response?.data?.errors);
      
      if (error.response?.data?.errors) {
        const errors = error.response.data.errors;
        const firstError = Object.values(errors)[0]?.[0];
        toast.error(firstError || 'Dönüştürme başarısız');
      } else if (error.response?.data?.message) {
        toast.error(error.response.data.message);
      } else {
        toast.error('Dönüştürme başarısız');
      }
    }
  };

  const getStatusBadge = (status) => {
    const config = {
      'Yeni': { icon: '🆕', text: 'Yeni', color: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300' },
      'IletisimeGecildi': { icon: '📞', text: 'İletişime Geçildi', color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300' },
      'TeklifSunuldu': { icon: '📄', text: 'Teklif Sunuldu', color: 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-300' },
      'MusteriOldu': { icon: '✅', text: 'Müşteri Oldu', color: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
      'Kaybedildi': { icon: '❌', text: 'Kaybedildi', color: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300' }
    };
    const c = config[status] || config['Yeni'];
    return <span className={`px-2 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
  };

  if (loading && page === 1) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Potansiyel Müşteriler</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Toplam {totalCount} lead
            {isConnected && <span className="ml-2 text-green-500 text-xs">🟢 Canlı</span>}
          </p>
        </div>
        <Link
          to="/leads/create"
          className="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg transition-colors flex items-center gap-2"
        >
          <span>+</span> Yeni Lead
        </Link>
      </div>

      {/* Filtreler */}
      <div className="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-800 p-4 mb-6">
        <div className="flex flex-wrap gap-3">
          <input
            type="text"
            placeholder="Firma, kişi, email, telefon ara..."
            value={filters.search}
            onChange={(e) => handleFilterChange('search', e.target.value)}
            className="flex-1 min-w-[200px] px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 text-sm"
          />
          <select
            value={filters.status}
            onChange={(e) => handleFilterChange('status', e.target.value)}
            className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm"
          >
            <option value="">Tüm Durumlar</option>
            {statusOptions.map(opt => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
          <select
            value={filters.source}
            onChange={(e) => handleFilterChange('source', e.target.value)}
            className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm"
          >
            <option value="">Tüm Kaynaklar</option>
            {sourceOptions.map(opt => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
          <select
            value={filters.assignedToPersonelId}
            onChange={(e) => handleFilterChange('assignedToPersonelId', e.target.value)}
            className="px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 text-sm"
          >
            <option value="">Tüm Personeller</option>
            {personels.map(p => (
              <option key={p.id} value={p.id}>{p.firstName} {p.lastName}</option>
            ))}
          </select>
          <button
            onClick={handleClearFilters}
            className="px-4 py-2 bg-gray-500 hover:bg-gray-600 text-white rounded-lg transition-colors text-sm font-medium"
          >
            Temizle
          </button>
        </div>
      </div>

      {/* Tablo */}
      <div className="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-800 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-800">
            <thead className="bg-gray-50 dark:bg-gray-800/50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Firma</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Yetkili</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İletişim</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Kampanya</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Atanan Personel</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Potansiyel Gelir</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tarih</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
              {leads.length === 0 ? (
                <tr>
                  <td colSpan={9} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                    Henüz lead bulunmuyor
                  </td>
                </tr>
              ) : (
                leads.map((lead) => (
                  <tr key={lead.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                    <td className="px-6 py-4 text-gray-900 dark:text-gray-100 font-medium">{lead.companyName}</td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-400">{lead.contactName}</td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-400 text-sm">
                      <div>{lead.email}</div>
                      <div className="text-xs">{lead.phone}</div>
                    </td>
                    <td className="px-6 py-4">{getStatusBadge(lead.status)}</td>
                    {/* 🔥 KAMPANYA SÜTUNU */}
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-400 text-sm">{lead.campaignName || '-'}</td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-400 text-sm">{lead.assignedToPersonelName || '-'}</td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-400">
                      {lead.potentialRevenue ? `${lead.potentialRevenue.toLocaleString('tr-TR')} ₺` : '-'}
                    </td>
                    <td className="px-6 py-4 text-gray-600 dark:text-gray-400 text-sm">
                      {new Date(lead.createdAt).toLocaleDateString('tr-TR')}
                    </td>
                    <td className="px-6 py-4 text-center">
                      <div className="flex justify-center gap-2">
                        <Link
                          to={`/leads/${lead.id}`}
                          className="text-emerald-600 dark:text-emerald-400 hover:text-emerald-800 dark:hover:text-emerald-300 transition-colors"
                          title="Detay"
                        >
                          👁️
                        </Link>
                        <Link
                          to={`/leads/edit/${lead.id}`}
                          className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition-colors"
                          title="Düzenle"
                        >
                          ✏️
                        </Link>
                        {lead.status !== 'MusteriOldu' && (
                          <button
                            onClick={() => openConvertModal(lead)}
                            className="text-green-600 dark:text-green-400 hover:text-green-800 dark:hover:text-green-300 transition-colors"
                            title="Müşteriye Dönüştür"
                          >
                            🔄
                          </button>
                        )}
                        <button
                          onClick={() => handleDelete(lead.id, lead.companyName)}
                          className="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300 transition-colors"
                          title="Sil"
                        >
                          🗑️
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
        <div className="flex justify-between items-center mt-4 pt-2">
          <div className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} kayıt</div>
          <div className="flex gap-2">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="px-3 py-1 border border-gray-200 dark:border-gray-700 rounded-lg disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">◀</button>
            <span className="px-3 py-1 text-sm text-gray-700 dark:text-gray-300">Sayfa {page} / {totalPages}</span>
            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="px-3 py-1 border border-gray-200 dark:border-gray-700 rounded-lg disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">▶</button>
          </div>
        </div>
      )}

      {/* Dönüştürme Modal */}
      {showConvertModal && selectedLead && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-md">
            <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
              <h2 className="text-xl font-bold text-gray-900 dark:text-white">Lead'i Müşteriye Dönüştür</h2>
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">{selectedLead.companyName} - {selectedLead.contactName}</p>
            </div>
            <div className="p-6 space-y-4">
              <input type="text" placeholder="Vergi No" value={convertForm.taxNumber} onChange={(e) => setConvertForm({...convertForm, taxNumber: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100" />
              <input type="text" placeholder="Vergi Dairesi" value={convertForm.taxOffice} onChange={(e) => setConvertForm({...convertForm, taxOffice: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100" />
              <input type="text" placeholder="Adres" value={convertForm.address} onChange={(e) => setConvertForm({...convertForm, address: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg" />
              <div className="grid grid-cols-2 gap-3">
                <input type="text" placeholder="Şehir" value={convertForm.city} onChange={(e) => setConvertForm({...convertForm, city: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg" />
                <input type="text" placeholder="İlçe" value={convertForm.district} onChange={(e) => setConvertForm({...convertForm, district: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg" />
              </div>
            </div>
            <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-200 dark:border-gray-700">
              <button onClick={() => setShowConvertModal(false)} className="px-4 py-2 border rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">İptal</button>
              <button onClick={handleConvert} className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg transition-colors">Dönüştür</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}