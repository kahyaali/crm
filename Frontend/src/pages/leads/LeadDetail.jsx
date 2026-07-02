// src/pages/leads/LeadDetail.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import leadApi from '../../services/leadApi';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function LeadDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [lead, setLead] = useState(null);
  const [loading, setLoading] = useState(true);
  const [showConvertModal, setShowConvertModal] = useState(false);
  const [convertForm, setConvertForm] = useState({
    taxNumber: '',
    taxOffice: '',
    address: '',
    city: '',
    district: ''
  });

  const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';

  useEffect(() => {
    fetchLead();
  }, [id]);

  const fetchLead = async () => {
    try {
      setLoading(true);
      const data = await leadApi.getById(id);
      setLead(data);
    } catch (error) {
      toast.error('Lead detayı yüklenemedi');
      navigate('/leads');
    } finally {
      setLoading(false);
    }
  };

  const handleConvert = async () => {
    try {
      await leadApi.convertToCustomer(id, convertForm);
      toast.success('Lead başarıyla müşteriye dönüştürüldü');
      setShowConvertModal(false);
      fetchLead();
    } catch (error) {
      toast.error(error.response?.data?.message || 'Dönüştürme başarısız');
    }
  };

  const getStatusBadge = (status) => {
    const config = {
      'Yeni': { icon: '🆕', text: 'Yeni', color: 'bg-blue-100 text-blue-800' },
      'IletisimeGecildi': { icon: '📞', text: 'İletişime Geçildi', color: 'bg-yellow-100 text-yellow-800' },
      'TeklifSunuldu': { icon: '📄', text: 'Teklif Sunuldu', color: 'bg-purple-100 text-purple-800' },
      'MusteriOldu': { icon: '✅', text: 'Müşteri Oldu', color: 'bg-green-100 text-green-800' },
      'Kaybedildi': { icon: '❌', text: 'Kaybedildi', color: 'bg-red-100 text-red-800' }
    };
    const c = config[status] || config['Yeni'];
    return <span className={`px-2 py-1 rounded-full text-xs font-medium ${c.color} dark:bg-opacity-30`}>{c.icon} {c.text}</span>;
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
      </div>
    );
  }

  if (!lead) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500">Lead bulunamadı</p>
        <Link to="/leads" className="mt-4 inline-block text-indigo-600">Lead listesine dön</Link>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <div className="flex justify-between items-start mb-6">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{lead.companyName}</h1>
            {getStatusBadge(lead.status)}
          </div>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Oluşturma: {new Date(lead.createdAt).toLocaleString('tr-TR')}
          </p>
        </div>
        <div className="flex gap-2">
          <Link to="/leads" className="px-4 py-2 bg-gray-500 hover:bg-gray-600 text-white rounded-lg text-sm">← Geri</Link>
          {lead.status !== 'MusteriOldu' && (
            <button onClick={() => setShowConvertModal(true)} className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg text-sm">🔄 Müşteriye Dönüştür</button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Firma Bilgileri</h2>
          <div className="space-y-3">
            <div><span className="text-sm text-gray-500 dark:text-gray-400">Firma Adı:</span> <span className="text-gray-900 dark:text-white">{lead.companyName}</span></div>
            <div><span className="text-sm text-gray-500 dark:text-gray-400">Yetkili Kişi:</span> <span className="text-gray-900 dark:text-white">{lead.contactName}</span></div>
            <div><span className="text-sm text-gray-500 dark:text-gray-400">Email:</span> <span className="text-gray-900 dark:text-white">{lead.email}</span></div>
            <div><span className="text-sm text-gray-500 dark:text-gray-400">Telefon:</span> <span className="text-gray-900 dark:text-white">{lead.phone}</span></div>
            <div><span className="text-sm text-gray-500 dark:text-gray-400">Kaynak:</span> <span className="text-gray-900 dark:text-white">{lead.source || '-'}</span></div>
            {/* 🔥 KAMPANYA BİLGİSİ EKLENDİ */}
            <div><span className="text-sm text-gray-500 dark:text-gray-400">Kampanya:</span> <span className="text-gray-900 dark:text-white">{lead.campaignName || '-'}</span></div>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Takip Bilgileri</h2>
          <div className="space-y-3">
            <div><span className="text-sm text-gray-500 dark:text-gray-400">Atanan Personel:</span> <span className="text-gray-900 dark:text-white">{lead.assignedToPersonelName || '-'}</span></div>
            <div><span className="text-sm text-gray-500 dark:text-gray-400">Potansiyel Gelir:</span> <span className="text-gray-900 dark:text-white">{lead.potentialRevenue ? `${lead.potentialRevenue.toLocaleString('tr-TR')} ₺` : '-'}</span></div>
            <div><span className="text-sm text-gray-500 dark:text-gray-400">Sonraki Takip:</span> <span className="text-gray-900 dark:text-white">{lead.nextFollowUpDate ? new Date(lead.nextFollowUpDate).toLocaleDateString('tr-TR') : '-'}</span></div>
            {lead.convertedToCustomerName && (
              <div><span className="text-sm text-gray-500 dark:text-gray-400">Dönüştürülen Müşteri:</span> <span className="text-gray-900 dark:text-white">{lead.convertedToCustomerName}</span></div>
            )}
          </div>
        </div>

        {lead.notes && (
          <div className="md:col-span-2 bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-6">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Notlar</h2>
            <p className="text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{lead.notes}</p>
          </div>
        )}
      </div>

      {/* Dönüştürme Modal */}
      {showConvertModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-md">
            <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
              <h2 className="text-xl font-bold text-gray-900 dark:text-white">Lead'i Müşteriye Dönüştür</h2>
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">{lead.companyName} - {lead.contactName}</p>
            </div>
            <div className="p-6 space-y-4">
              <input type="text" placeholder="Vergi No" value={convertForm.taxNumber} onChange={(e) => setConvertForm({...convertForm, taxNumber: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg" />
              <input type="text" placeholder="Vergi Dairesi" value={convertForm.taxOffice} onChange={(e) => setConvertForm({...convertForm, taxOffice: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg" />
              <input type="text" placeholder="Adres" value={convertForm.address} onChange={(e) => setConvertForm({...convertForm, address: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg" />
              <div className="grid grid-cols-2 gap-3">
                <input type="text" placeholder="Şehir" value={convertForm.city} onChange={(e) => setConvertForm({...convertForm, city: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg" />
                <input type="text" placeholder="İlçe" value={convertForm.district} onChange={(e) => setConvertForm({...convertForm, district: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg" />
              </div>
            </div>
            <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-200 dark:border-gray-700">
              <button onClick={() => setShowConvertModal(false)} className="px-4 py-2 border rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700">İptal</button>
              <button onClick={handleConvert} className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg">Dönüştür</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}