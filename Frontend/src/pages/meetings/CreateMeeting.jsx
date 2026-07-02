// src/pages/meetings/CreateMeeting.jsx
import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function CreateMeeting() {
    const navigate = useNavigate();
    const { user } = useAuth();
    const [loading, setLoading] = useState(false);
    const [customers, setCustomers] = useState([]);
    const [leads, setLeads] = useState([]);
    const [personels, setPersonels] = useState([]);
    const [statusOptions, setStatusOptions] = useState([]);
    
    // Personel modal states
    const [showPersonelModal, setShowPersonelModal] = useState(false);
    const [selectedPersonelIds, setSelectedPersonelIds] = useState([]);
    const [personelSearch, setPersonelSearch] = useState('');
    const [personelPage, setPersonelPage] = useState(1);
    const [personelTotalPages, setPersonelTotalPages] = useState(1);
    
    // Müşteri modal states
    const [showCustomerModal, setShowCustomerModal] = useState(false);
    const [selectedCustomerId, setSelectedCustomerId] = useState('');
    const [customerSearch, setCustomerSearch] = useState('');
    const [customerPage, setCustomerPage] = useState(1);
    const [customerTotalPages, setCustomerTotalPages] = useState(1);
    const [allCustomers, setAllCustomers] = useState([]);
    
    // Lead modal states
    const [showLeadModal, setShowLeadModal] = useState(false);
    const [selectedLeadId, setSelectedLeadId] = useState('');
    const [leadSearch, setLeadSearch] = useState('');
    const [leadPage, setLeadPage] = useState(1);
    const [leadTotalPages, setLeadTotalPages] = useState(1);
    const [allLeads, setAllLeads] = useState([]);
    
    const [formData, setFormData] = useState({
        title: '',
        description: '',
        startTime: '',
        endTime: '',
        location: '',
        meetingLink: '',
        customerId: '',
        leadId: '',
        status: 'Planlandı',
        notes: '',
        attendeePersonelIds: []
    });

    useEffect(() => {
        fetchCustomers();
        fetchLeads();
        fetchPersonels();
        fetchStatusList();
    }, []);

    useEffect(() => {
        if (showPersonelModal) {
            fetchPersonelsForModal();
        }
    }, [showPersonelModal, personelSearch, personelPage]);

    useEffect(() => {
        if (showCustomerModal) {
            fetchCustomersForModal();
        }
    }, [showCustomerModal, customerSearch, customerPage]);

    useEffect(() => {
        if (showLeadModal) {
            fetchLeadsForModal();
        }
    }, [showLeadModal, leadSearch, leadPage]);

    const fetchCustomers = async () => {
        try {
            const response = await api.get('/meetings/customer-list');
            const customerList = response.data || [];
            setCustomers(customerList);
            setAllCustomers(customerList);
        } catch (error) {
            console.error('Müşteriler yüklenemedi:', error);
            toast.error('Müşteri listesi yüklenemedi');
        }
    };

    const fetchLeads = async () => {
        try {
            const response = await api.get('/meetings/lead-list');
            const leadList = response.data || [];
            setLeads(leadList);
            setAllLeads(leadList);
        } catch (error) {
            console.error('Leadler yüklenemedi:', error);
            toast.error('Lead listesi yüklenemedi');
        }
    };

    const fetchCustomersForModal = async () => {
        try {
            const response = await api.get('/meetings/customer-list', {
                params: {
                    page: customerPage,
                    pageSize: 10,
                    search: customerSearch
                }
            });
            const customerList = response.data.data || response.data || [];
            setAllCustomers(customerList);
            setCustomerTotalPages(response.data.totalPages || 1);
        } catch (error) {
            console.error('Müşteriler yüklenemedi:', error);
            const response = await api.get('/meetings/customer-list');
            setAllCustomers(response.data || []);
        }
    };

    const fetchLeadsForModal = async () => {
        try {
            const response = await api.get('/meetings/lead-list', {
                params: {
                    page: leadPage,
                    pageSize: 10,
                    search: leadSearch
                }
            });
            const leadList = response.data.data || response.data || [];
            setAllLeads(leadList);
            setLeadTotalPages(response.data.totalPages || 1);
        } catch (error) {
            console.error('Leadler yüklenemedi:', error);
            const response = await api.get('/meetings/lead-list');
            setAllLeads(response.data || []);
        }
    };

    const fetchPersonels = async () => {
        try {
            const response = await api.get('/tickets/personel-list');
            setPersonels(response.data || []);
        } catch (error) {
            console.error('Personeller yüklenemedi:', error);
        }
    };

    const fetchPersonelsForModal = async () => {
        try {
            const response = await api.get('/Personels', {
                params: {
                    page: personelPage,
                    pageSize: 10,
                    search: personelSearch,
                    isActive: true
                }
            });
            setPersonels(response.data.data || []);
            setPersonelTotalPages(response.data.totalPages || 1);
        } catch (error) {
            console.error('Personeller yüklenemedi:', error);
        }
    };

    const fetchStatusList = async () => {
        try {
            const response = await api.get('/meetings/status-list');
            setStatusOptions(response.data);
        } catch (error) {
            console.error('Durum listesi yüklenemedi:', error);
        }
    };

    const handleAddAttendees = () => {
        setFormData(prev => ({
            ...prev,
            attendeePersonelIds: [...new Set([...prev.attendeePersonelIds, ...selectedPersonelIds])]
        }));
        setSelectedPersonelIds([]);
        setShowPersonelModal(false);
        setPersonelSearch('');
        setPersonelPage(1);
    };

    const handleRemoveAttendee = (personelId) => {
        setFormData(prev => ({
            ...prev,
            attendeePersonelIds: prev.attendeePersonelIds.filter(id => id !== personelId)
        }));
    };

    const handleTogglePersonelSelection = (personelId) => {
        setSelectedPersonelIds(prev =>
            prev.includes(personelId)
                ? prev.filter(id => id !== personelId)
                : [...prev, personelId]
        );
    };

    const handleSelectCustomer = () => {
        setFormData(prev => ({
            ...prev,
            customerId: selectedCustomerId
        }));
        setSelectedCustomerId('');
        setShowCustomerModal(false);
        setCustomerSearch('');
        setCustomerPage(1);
    };

    const handleSelectLead = () => {
        setFormData(prev => ({
            ...prev,
            leadId: selectedLeadId
        }));
        setSelectedLeadId('');
        setShowLeadModal(false);
        setLeadSearch('');
        setLeadPage(1);
    };

    const handleRemoveCustomer = () => {
        setFormData(prev => ({ ...prev, customerId: '' }));
    };

    const handleRemoveLead = () => {
        setFormData(prev => ({ ...prev, leadId: '' }));
    };

    const selectedAttendees = personels.filter(p => formData.attendeePersonelIds.includes(p.id));
    const selectedCustomer = allCustomers.find(c => c.id === formData.customerId) || customers.find(c => c.id === formData.customerId);
    const selectedLead = allLeads.find(l => l.id === formData.leadId) || leads.find(l => l.id === formData.leadId);

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        if (!formData.title.trim()) {
            toast.error('Toplantı başlığı zorunludur');
            return;
        }
        if (!formData.startTime) {
            toast.error('Başlangıç zamanı zorunludur');
            return;
        }
        if (!formData.endTime) {
            toast.error('Bitiş zamanı zorunludur');
            return;
        }
        if (new Date(formData.startTime) >= new Date(formData.endTime)) {
            toast.error('Bitiş zamanı, başlangıç zamanından sonra olmalıdır');
            return;
        }
        if (formData.attendeePersonelIds.length === 0) {
            toast.error('En az bir katılımcı seçmelisiniz');
            return;
        }

        setLoading(true);
        try {
            await api.post('/meetings', formData);
            toast.success('✅ Toplantı başarıyla oluşturuldu');
            navigate('/meetings');
        } catch (error) {
            toast.error(error.response?.data?.message || 'Toplantı oluşturulamadı');
        } finally {
            setLoading(false);
        }
    };

    const inputStyle = "w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 transition-all duration-200 text-sm";
    const labelStyle = "text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block";

    // Müşteri adını formatla
    const getCustomerDisplayName = (customer) => {
        if (!customer) return '';
        const name = `${customer.firstName || ''} ${customer.lastName || ''}`.trim();
        return name ? `${name} ${customer.companyName ? `(${customer.companyName})` : ''}` : customer.companyName || 'İsimsiz Müşteri';
    };

    // Lead adını formatla
    const getLeadDisplayName = (lead) => {
        if (!lead) return '';
        return lead.companyName ? `${lead.companyName} ${lead.contactName ? `- ${lead.contactName}` : ''}` : lead.contactName || 'İsimsiz Lead';
    };

    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Yeni Toplantı</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
                        Yeni toplantı planlayın ve katılımcıları davet edin
                    </p>
                </div>
                <Link to="/meetings" className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition">
                    <span>←</span> Toplantılara Dön
                </Link>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm overflow-hidden">
                    <div className="px-5 py-3 bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-700">
                        <div className="flex items-center gap-2">
                            <span className="text-base">📅</span>
                            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">
                                Toplantı Bilgileri
                            </h2>
                        </div>
                    </div>
                    
                    <div className="p-5 space-y-4">
                        {/* Başlık ve Açıklama */}
                        <div>
                            <label className={labelStyle}>Toplantı Başlığı <span className="text-red-500">*</span></label>
                            <input type="text" value={formData.title} onChange={(e) => setFormData({...formData, title: e.target.value})} className={inputStyle} required />
                        </div>

                        <div>
                            <label className={labelStyle}>Açıklama</label>
                            <textarea rows="3" value={formData.description} onChange={(e) => setFormData({...formData, description: e.target.value})} className={`${inputStyle} resize-none`} />
                        </div>

                        {/* Tarih/Saat */}
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Başlangıç Zamanı <span className="text-red-500">*</span></label>
                                <input type="datetime-local" value={formData.startTime} onChange={(e) => setFormData({...formData, startTime: e.target.value})} className={inputStyle} required />
                            </div>
                            <div>
                                <label className={labelStyle}>Bitiş Zamanı <span className="text-red-500">*</span></label>
                                <input type="datetime-local" value={formData.endTime} onChange={(e) => setFormData({...formData, endTime: e.target.value})} className={inputStyle} required />
                            </div>
                        </div>

                        {/* Konum ve Link */}
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Konum</label>
                                <input type="text" placeholder="Toplantı odası, ofis adresi..." value={formData.location} onChange={(e) => setFormData({...formData, location: e.target.value})} className={inputStyle} />
                            </div>
                            <div>
                                <label className={labelStyle}>Toplantı Linki</label>
                                <input type="url" placeholder="https://zoom.us/..." value={formData.meetingLink} onChange={(e) => setFormData({...formData, meetingLink: e.target.value})} className={inputStyle} />
                            </div>
                        </div>

                        {/* İlgili Müşteri ve Lead - Modal ile seçim */}
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            {/* Müşteri Seçimi */}
                            <div>
                                <label className={labelStyle}>İlgili Müşteri</label>
                                {selectedCustomer ? (
                                    <div className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                                        <div className="w-8 h-8 rounded-full bg-gradient-to-br from-amber-500 to-orange-500 flex items-center justify-center text-white text-sm font-medium">
                                            {selectedCustomer.firstName?.charAt(0) || selectedCustomer.companyName?.charAt(0) || 'M'}
                                        </div>
                                        <div className="flex-1">
                                            <p className="text-sm font-medium text-gray-900 dark:text-white">
                                                {getCustomerDisplayName(selectedCustomer)}
                                            </p>
                                            {selectedCustomer.email && (
                                                <p className="text-xs text-gray-500 dark:text-gray-400">{selectedCustomer.email}</p>
                                            )}
                                        </div>
                                        <button
                                            type="button"
                                            onClick={handleRemoveCustomer}
                                            className="text-gray-400 hover:text-red-500 transition-colors"
                                        >
                                            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                            </svg>
                                        </button>
                                    </div>
                                ) : (
                                    <button
                                        type="button"
                                        onClick={() => {
                                            setSelectedCustomerId('');
                                            setCustomerSearch('');
                                            setCustomerPage(1);
                                            setShowCustomerModal(true);
                                        }}
                                        className="w-full py-2 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg text-gray-500 dark:text-gray-400 hover:border-amber-500 hover:text-amber-600 transition-colors flex items-center justify-center gap-2"
                                    >
                                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                                        </svg>
                                        Müşteri Seç
                                    </button>
                                )}
                            </div>

                            {/* Lead Seçimi */}
                            <div>
                                <label className={labelStyle}>İlgili Lead</label>
                                {selectedLead ? (
                                    <div className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                                        <div className="w-8 h-8 rounded-full bg-gradient-to-br from-cyan-500 to-blue-500 flex items-center justify-center text-white text-sm font-medium">
                                            {selectedLead.companyName?.charAt(0) || selectedLead.contactName?.charAt(0) || 'L'}
                                        </div>
                                        <div className="flex-1">
                                            <p className="text-sm font-medium text-gray-900 dark:text-white">
                                                {getLeadDisplayName(selectedLead)}
                                            </p>
                                            {selectedLead.email && (
                                                <p className="text-xs text-gray-500 dark:text-gray-400">{selectedLead.email}</p>
                                            )}
                                        </div>
                                        <button
                                            type="button"
                                            onClick={handleRemoveLead}
                                            className="text-gray-400 hover:text-red-500 transition-colors"
                                        >
                                            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                            </svg>
                                        </button>
                                    </div>
                                ) : (
                                    <button
                                        type="button"
                                        onClick={() => {
                                            setSelectedLeadId('');
                                            setLeadSearch('');
                                            setLeadPage(1);
                                            setShowLeadModal(true);
                                        }}
                                        className="w-full py-2 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg text-gray-500 dark:text-gray-400 hover:border-cyan-500 hover:text-cyan-600 transition-colors flex items-center justify-center gap-2"
                                    >
                                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                                        </svg>
                                        Lead Seç
                                    </button>
                                )}
                            </div>
                        </div>

                        {/* Durum ve Notlar */}
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className={labelStyle}>Durum</label>
                                <select value={formData.status} onChange={(e) => setFormData({...formData, status: e.target.value})} className={inputStyle}>
                                    {statusOptions.map(opt => (
                                        <option key={opt.value} value={opt.value}>{opt.label}</option>
                                    ))}
                                </select>
                            </div>
                            <div>
                                <label className={labelStyle}>Notlar</label>
                                <input type="text" value={formData.notes} onChange={(e) => setFormData({...formData, notes: e.target.value})} className={inputStyle} />
                            </div>
                        </div>

                        {/* Katılımcılar */}
                       {/* Katılımcılar */}
<div>
    <label className={labelStyle}>Katılımcılar <span className="text-red-500">*</span></label>
    
    {/* 🔥 BİLGİ MESAJI */}
    <div className="mb-3 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
        <div className="flex items-center gap-2 text-blue-700 dark:text-blue-300">
            <svg className="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span className="text-sm">
                <strong>{user?.firstName} {user?.lastName}</strong> toplantıyı oluşturan kişi olarak otomatik katılımcı olarak eklenecektir.
            </span>
        </div>
    </div>
    
    {/* Seçilen katılımcılar - kart görünümü */}
    <div className="mb-3 p-3 bg-gray-50 dark:bg-gray-800/30 rounded-lg min-h-[80px]">
        {selectedAttendees.length > 0 ? (
            <div className="flex flex-wrap gap-2">
                {selectedAttendees.map(attendee => (
                    <div key={attendee.id} className="flex items-center gap-2 px-3 py-1.5 bg-white dark:bg-gray-700 rounded-lg shadow-sm border border-gray-200 dark:border-gray-600">
                        <div className="w-6 h-6 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-xs font-medium text-white">
                            {attendee.firstName?.charAt(0)}{attendee.lastName?.charAt(0)}
                        </div>
                        <span className="text-sm text-gray-700 dark:text-gray-300">
                            {attendee.firstName} {attendee.lastName}
                        </span>
                        <button
                            type="button"
                            onClick={() => handleRemoveAttendee(attendee.id)}
                            className="text-gray-400 hover:text-red-500 transition-colors"
                        >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                            </svg>
                        </button>
                    </div>
                ))}
            </div>
        ) : (
            <div className="text-center text-gray-400 text-sm py-2">
                Henüz katılımcı seçilmedi
            </div>
        )}
    </div>

    {/* Katılımcı Ekle Butonu */}
    <button
        type="button"
        onClick={() => {
            setSelectedPersonelIds([]);
            setPersonelSearch('');
            setPersonelPage(1);
            setShowPersonelModal(true);
        }}
        className="w-full py-2 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg text-gray-500 dark:text-gray-400 hover:border-indigo-500 hover:text-indigo-600 transition-colors flex items-center justify-center gap-2"
    >
        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
        </svg>
        Katılımcı Ekle
    </button>
</div>
                    </div>
                </div>

                <div className="flex justify-end gap-3 pt-2">
                    <Link to="/meetings" className="px-4 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-200 font-medium rounded-lg text-sm hover:bg-gray-100 dark:hover:bg-gray-700 transition">İptal</Link>
                    <button type="submit" disabled={loading} className="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-400 dark:disabled:bg-indigo-800 text-white font-semibold rounded-lg text-sm transition disabled:cursor-not-allowed flex items-center gap-1">
                        {loading ? 'Oluşturuluyor...' : 'Toplantı Oluştur'}
                    </button>
                </div>
            </form>

            {/* Katılımcı Seçme Modalı */}
            {showPersonelModal && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-2xl max-h-[80vh] flex flex-col">
                        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center">
                            <h2 className="text-xl font-bold text-gray-900 dark:text-white">Katılımcı Seç</h2>
                            <button type="button" onClick={() => setShowPersonelModal(false)} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
                                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>
                        </div>
                        <div className="p-4 border-b border-gray-200 dark:border-gray-700">
                            <input type="text" placeholder="Personel ara (isim, soyisim, email)..." value={personelSearch} onChange={(e) => setPersonelSearch(e.target.value)} className="w-full px-4 py-2 bg-gray-100 dark:bg-gray-700 rounded-lg text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500" />
                        </div>
                        <div className="flex-1 overflow-y-auto p-4">
                            <div className="space-y-2">
                                {personels.map(personel => (
                                    <label key={personel.id} className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors ${selectedPersonelIds.includes(personel.id) ? 'bg-indigo-50 dark:bg-indigo-900/30 border border-indigo-200 dark:border-indigo-800' : 'hover:bg-gray-50 dark:hover:bg-gray-700/50 border border-transparent'}`}>
                                        <input type="checkbox" checked={selectedPersonelIds.includes(personel.id)} onChange={() => handleTogglePersonelSelection(personel.id)} className="w-4 h-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500" />
                                        <div className="flex-1">
                                            <p className="font-medium text-gray-900 dark:text-white">{personel.firstName} {personel.lastName}</p>
                                            <p className="text-sm text-gray-500 dark:text-gray-400">{personel.email}</p>
                                        </div>
                                        {personel.departmentName && <span className="text-xs text-gray-400 dark:text-gray-500">{personel.departmentName}</span>}
                                    </label>
                                ))}
                            </div>
                        </div>
                        <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex justify-between items-center">
                            <div className="text-sm text-gray-500 dark:text-gray-400">{selectedPersonelIds.length} kişi seçildi</div>
                            <div className="flex gap-2">
                                <button type="button" onClick={() => setShowPersonelModal(false)} className="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition">İptal</button>
                                <button type="button" onClick={handleAddAttendees} className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg transition">Ekle ({selectedPersonelIds.length})</button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {/* Müşteri Seçme Modalı */}
            {showCustomerModal && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-2xl max-h-[80vh] flex flex-col">
                        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center">
                            <h2 className="text-xl font-bold text-gray-900 dark:text-white">Müşteri Seç</h2>
                            <button type="button" onClick={() => setShowCustomerModal(false)} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
                                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>
                        </div>
                        <div className="p-4 border-b border-gray-200 dark:border-gray-700">
                            <input type="text" placeholder="Müşteri ara (isim, şirket, email)..." value={customerSearch} onChange={(e) => setCustomerSearch(e.target.value)} className="w-full px-4 py-2 bg-gray-100 dark:bg-gray-700 rounded-lg text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-amber-500" />
                        </div>
                        <div className="flex-1 overflow-y-auto p-4">
                            <div className="space-y-2">
                                {allCustomers.map(customer => (
                                    <label key={customer.id} className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors ${selectedCustomerId === customer.id ? 'bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-800' : 'hover:bg-gray-50 dark:hover:bg-gray-700/50 border border-transparent'}`}>
                                        <input type="radio" name="customer" checked={selectedCustomerId === customer.id} onChange={() => setSelectedCustomerId(customer.id)} className="w-4 h-4 text-amber-600 focus:ring-amber-500" />
                                        <div className="w-10 h-10 rounded-full bg-gradient-to-br from-amber-500 to-orange-500 flex items-center justify-center text-white font-medium">
                                            {customer.firstName?.charAt(0) || customer.companyName?.charAt(0) || 'M'}
                                        </div>
                                        <div className="flex-1">
                                            <p className="font-medium text-gray-900 dark:text-white">
                                                {getCustomerDisplayName(customer)}
                                            </p>
                                            {customer.email && <p className="text-sm text-gray-500 dark:text-gray-400">{customer.email}</p>}
                                            {customer.phone && <p className="text-xs text-gray-400 dark:text-gray-500">{customer.phone}</p>}
                                        </div>
                                    </label>
                                ))}
                                {allCustomers.length === 0 && (
                                    <div className="text-center py-8 text-gray-400">Müşteri bulunamadı</div>
                                )}
                            </div>
                        </div>
                        <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex justify-between items-center">
                            <div className="text-sm text-gray-500 dark:text-gray-400">{selectedCustomerId ? '1 müşteri seçildi' : 'Müşteri seçilmedi'}</div>
                            <div className="flex gap-2">
                                <button type="button" onClick={() => setShowCustomerModal(false)} className="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition">İptal</button>
                                <button type="button" onClick={handleSelectCustomer} disabled={!selectedCustomerId} className="px-4 py-2 bg-amber-600 hover:bg-amber-700 disabled:bg-amber-400 text-white rounded-lg transition">Seç</button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {/* Lead Seçme Modalı */}
            {showLeadModal && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-2xl max-h-[80vh] flex flex-col">
                        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center">
                            <h2 className="text-xl font-bold text-gray-900 dark:text-white">Lead Seç</h2>
                            <button type="button" onClick={() => setShowLeadModal(false)} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
                                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>
                        </div>
                        <div className="p-4 border-b border-gray-200 dark:border-gray-700">
                            <input type="text" placeholder="Lead ara (şirket, kişi, email)..." value={leadSearch} onChange={(e) => setLeadSearch(e.target.value)} className="w-full px-4 py-2 bg-gray-100 dark:bg-gray-700 rounded-lg text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-cyan-500" />
                        </div>
                        <div className="flex-1 overflow-y-auto p-4">
                            <div className="space-y-2">
                                {allLeads.map(lead => (
                                    <label key={lead.id} className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors ${selectedLeadId === lead.id ? 'bg-cyan-50 dark:bg-cyan-900/30 border border-cyan-200 dark:border-cyan-800' : 'hover:bg-gray-50 dark:hover:bg-gray-700/50 border border-transparent'}`}>
                                        <input type="radio" name="lead" checked={selectedLeadId === lead.id} onChange={() => setSelectedLeadId(lead.id)} className="w-4 h-4 text-cyan-600 focus:ring-cyan-500" />
                                        <div className="w-10 h-10 rounded-full bg-gradient-to-br from-cyan-500 to-blue-500 flex items-center justify-center text-white font-medium">
                                            {lead.companyName?.charAt(0) || lead.contactName?.charAt(0) || 'L'}
                                        </div>
                                        <div className="flex-1">
                                            <p className="font-medium text-gray-900 dark:text-white">{getLeadDisplayName(lead)}</p>
                                            {lead.email && <p className="text-sm text-gray-500 dark:text-gray-400">{lead.email}</p>}
                                            {lead.phone && <p className="text-xs text-gray-400 dark:text-gray-500">{lead.phone}</p>}
                                        </div>
                                    </label>
                                ))}
                                {allLeads.length === 0 && (
                                    <div className="text-center py-8 text-gray-400">Lead bulunamadı</div>
                                )}
                            </div>
                        </div>
                        <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex justify-between items-center">
                            <div className="text-sm text-gray-500 dark:text-gray-400">{selectedLeadId ? '1 lead seçildi' : 'Lead seçilmedi'}</div>
                            <div className="flex gap-2">
                                <button type="button" onClick={() => setShowLeadModal(false)} className="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition">İptal</button>
                                <button type="button" onClick={handleSelectLead} disabled={!selectedLeadId} className="px-4 py-2 bg-cyan-600 hover:bg-cyan-700 disabled:bg-cyan-400 text-white rounded-lg transition">Seç</button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}