// src/pages/meetings/EditMeeting.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';

export default function EditMeeting() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [loading, setLoading] = useState(false);
    const [pageLoading, setPageLoading] = useState(true);
    const [customers, setCustomers] = useState([]);
    const [leads, setLeads] = useState([]);
    const [personels, setPersonels] = useState([]);
    const [statusOptions, setStatusOptions] = useState([]);
    
    // Personel modal states
    const [showPersonelModal, setShowPersonelModal] = useState(false);
    const [selectedPersonelIds, setSelectedPersonelIds] = useState([]);
    const [personelSearch, setPersonelSearch] = useState('');
    
    // Müşteri modal states
    const [showCustomerModal, setShowCustomerModal] = useState(false);
    const [selectedCustomerId, setSelectedCustomerId] = useState('');
    const [customerSearch, setCustomerSearch] = useState('');
    
    // Lead modal states
    const [showLeadModal, setShowLeadModal] = useState(false);
    const [selectedLeadId, setSelectedLeadId] = useState('');
    const [leadSearch, setLeadSearch] = useState('');
    
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

    const currentPersonelId = user?.personelId;

    useEffect(() => {
        fetchMeetingData();
        fetchCustomers();
        fetchLeads();
        fetchPersonels();
        fetchStatusList();
    }, [id]);

    const fetchMeetingData = async () => {
        try {
            setPageLoading(true);
            const response = await api.get(`/meetings/${id}`);
            const meeting = response.data;
            
            if (String(meeting.createdByPersonelId) !== String(currentPersonelId)) {
                toast.error('Sadece toplantıyı açan kişi düzenleyebilir.');
                navigate('/meetings');
                return;
            }
            
            setFormData({
                title: meeting.title || '',
                description: meeting.description || '',
                startTime: meeting.startTime ? meeting.startTime.slice(0, 16) : '',
                endTime: meeting.endTime ? meeting.endTime.slice(0, 16) : '',
                location: meeting.location || '',
                meetingLink: meeting.meetingLink || '',
                customerId: meeting.customerId || '',
                leadId: meeting.leadId || '',
                status: meeting.status || 'Planlandı',
                notes: meeting.notes || '',
                attendeePersonelIds: meeting.attendees?.map(a => a.personelId) || []
            });
        } catch (error) {
            toast.error('Toplantı bilgileri yüklenemedi');
            navigate('/meetings');
        } finally {
            setPageLoading(false);
        }
    };

    const fetchCustomers = async () => {
        try {
            const response = await api.get('/meetings/customer-list');
            setCustomers(response.data || []);
        } catch (error) {
            console.error('Müşteriler yüklenemedi:', error);
        }
    };

    const fetchLeads = async () => {
        try {
            const response = await api.get('/meetings/lead-list');
            setLeads(response.data || []);
        } catch (error) {
            console.error('Leadler yüklenemedi:', error);
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
    };

 const handleRemoveAttendee = (personelId) => {
    // 🔥 TOPLANTI SAHİBİ KENDİNİ ÇIKARAMAZ
    if (personelId === currentPersonelId) {
        toast.error('Toplantı sahibi kendini katılımcılardan çıkaramaz.');
        return;
    }
    setFormData(prev => ({
        ...prev,
        attendeePersonelIds: prev.attendeePersonelIds.filter(id => id !== personelId)
    }));
};

    const handleTogglePersonelSelection = (personelId) => {
        setSelectedPersonelIds(prev =>
            prev.includes(personelId) ? prev.filter(id => id !== personelId) : [...prev, personelId]
        );
    };

    const handleSelectCustomer = () => {
        setFormData(prev => ({ ...prev, customerId: selectedCustomerId }));
        setSelectedCustomerId('');
        setShowCustomerModal(false);
        setCustomerSearch('');
    };

    const handleSelectLead = () => {
        setFormData(prev => ({ ...prev, leadId: selectedLeadId }));
        setSelectedLeadId('');
        setShowLeadModal(false);
        setLeadSearch('');
    };

    const handleRemoveCustomer = () => {
        setFormData(prev => ({ ...prev, customerId: '' }));
    };

    const handleRemoveLead = () => {
        setFormData(prev => ({ ...prev, leadId: '' }));
    };

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

        const submitData = {
            id: Number(id),
            title: formData.title,
            description: formData.description || null,
            startTime: formData.startTime,
            endTime: formData.endTime,
            location: formData.location || null,
            meetingLink: formData.meetingLink || null,
            customerId: formData.customerId ? Number(formData.customerId) : null,
            leadId: formData.leadId ? Number(formData.leadId) : null,
            status: formData.status,
            notes: formData.notes || null,
            attendeePersonelIds: formData.attendeePersonelIds.map(id => Number(id))
        };

        setLoading(true);
        try {
            await api.put(`/meetings/${id}`, submitData);
            toast.success('✅ Toplantı başarıyla güncellendi');
            navigate('/meetings');
        } catch (error) {
            toast.error(error.response?.data?.message || 'Toplantı güncellenemedi');
        } finally {
            setLoading(false);
        }
    };

    const selectedAttendees = personels.filter(p => formData.attendeePersonelIds.includes(p.id));
    const selectedCustomer = customers.find(c => c.id === formData.customerId);
    const selectedLead = leads.find(l => l.id === formData.leadId);

    const getCustomerDisplayName = (customer) => {
        if (!customer) return '';
        const name = `${customer.firstName || ''} ${customer.lastName || ''}`.trim();
        return name ? `${name} ${customer.companyName ? `(${customer.companyName})` : ''}` : customer.companyName || 'İsimsiz Müşteri';
    };

    const getLeadDisplayName = (lead) => {
        if (!lead) return '';
        return lead.companyName ? `${lead.companyName} ${lead.contactName ? `- ${lead.contactName}` : ''}` : lead.contactName || 'İsimsiz Lead';
    };

    if (pageLoading) {
        return (
            <div className="flex justify-center items-center h-64">
                <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Toplantı Düzenle</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400">{formData.title} toplantısını düzenleyin</p>
                </div>
                <Link to="/meetings" className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition">← Toplantılara Dön</Link>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm overflow-hidden">
                    <div className="px-5 py-3 bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-700">
                        <div className="flex items-center gap-2">
                            <span className="text-base">📅</span>
                            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">Toplantı Bilgileri</h2>
                        </div>
                    </div>
                    
                    <div className="p-5 space-y-4">
                        <div>
                            <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">Toplantı Başlığı <span className="text-red-500">*</span></label>
                            <input type="text" value={formData.title} onChange={(e) => setFormData({...formData, title: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg" required />
                        </div>

                        <div>
                            <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">Açıklama</label>
                            <textarea rows="3" value={formData.description} onChange={(e) => setFormData({...formData, description: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg resize-none" />
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">Başlangıç Zamanı <span className="text-red-500">*</span></label>
                                <input type="datetime-local" value={formData.startTime} onChange={(e) => setFormData({...formData, startTime: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg" required />
                            </div>
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">Bitiş Zamanı <span className="text-red-500">*</span></label>
                                <input type="datetime-local" value={formData.endTime} onChange={(e) => setFormData({...formData, endTime: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg" required />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">Konum</label>
                                <input type="text" placeholder="Toplantı odası, ofis adresi..." value={formData.location} onChange={(e) => setFormData({...formData, location: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg" />
                            </div>
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">Toplantı Linki</label>
                                <input type="url" placeholder="https://..." value={formData.meetingLink} onChange={(e) => setFormData({...formData, meetingLink: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg" />
                            </div>
                        </div>

                        {/* Müşteri Seçimi */}
                        <div>
                            <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">İlgili Müşteri</label>
                            {selectedCustomer ? (
                                <div className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
                                    <div className="w-8 h-8 rounded-full bg-gradient-to-br from-amber-500 to-orange-500 flex items-center justify-center text-white text-sm">
                                        {selectedCustomer.firstName?.charAt(0) || 'M'}
                                    </div>
                                    <div className="flex-1">
                                        <p className="text-sm font-medium">{getCustomerDisplayName(selectedCustomer)}</p>
                                        {selectedCustomer.email && <p className="text-xs text-gray-500">{selectedCustomer.email}</p>}
                                    </div>
                                    <button type="button" onClick={handleRemoveCustomer} className="text-gray-400 hover:text-red-500">✕</button>
                                </div>
                            ) : (
                                <button type="button" onClick={() => setShowCustomerModal(true)} className="w-full py-2 border-2 border-dashed border-gray-300 rounded-lg text-gray-500 hover:border-amber-500 hover:text-amber-600 flex items-center justify-center gap-2">
                                    + Müşteri Seç
                                </button>
                            )}
                        </div>

                        {/* Lead Seçimi */}
                        <div>
                            <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">İlgili Lead</label>
                            {selectedLead ? (
                                <div className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
                                    <div className="w-8 h-8 rounded-full bg-gradient-to-br from-cyan-500 to-blue-500 flex items-center justify-center text-white text-sm">
                                        {selectedLead.companyName?.charAt(0) || 'L'}
                                    </div>
                                    <div className="flex-1">
                                        <p className="text-sm font-medium">{getLeadDisplayName(selectedLead)}</p>
                                        {selectedLead.email && <p className="text-xs text-gray-500">{selectedLead.email}</p>}
                                    </div>
                                    <button type="button" onClick={handleRemoveLead} className="text-gray-400 hover:text-red-500">✕</button>
                                </div>
                            ) : (
                                <button type="button" onClick={() => setShowLeadModal(true)} className="w-full py-2 border-2 border-dashed border-gray-300 rounded-lg text-gray-500 hover:border-cyan-500 hover:text-cyan-600 flex items-center justify-center gap-2">
                                    + Lead Seç
                                </button>
                            )}
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">Durum</label>
                                <select value={formData.status} onChange={(e) => setFormData({...formData, status: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg">
                                    {statusOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                                </select>
                            </div>
                            <div>
                                <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">Notlar</label>
                                <input type="text" value={formData.notes} onChange={(e) => setFormData({...formData, notes: e.target.value})} className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg" />
                            </div>
                        </div>

                        {/* Katılımcılar - CreateMeeting'deki gibi modal ile */}
                        <div>
                            <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">Katılımcılar</label>
                            
                            <div className="mb-3 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200">
                                <div className="flex items-center gap-2 text-blue-700">
                                    <span>ℹ️</span>
                                    <span className="text-sm"><strong>{user?.firstName} {user?.lastName}</strong> toplantıyı oluşturan kişi olarak otomatik katılımcıdır.</span>
                                </div>
                            </div>
                            
                            <div className="mb-3 p-3 bg-gray-50 dark:bg-gray-800/30 rounded-lg min-h-[80px]">
                                {selectedAttendees.length > 0 ? (
                                    <div className="flex flex-wrap gap-2">
                                        {selectedAttendees.map(attendee => (
                                            <div key={attendee.id} className="flex items-center gap-2 px-3 py-1.5 bg-white rounded-lg shadow-sm border">
                                                <div className="w-6 h-6 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-xs text-white">
                                                    {attendee.firstName?.charAt(0)}{attendee.lastName?.charAt(0)}
                                                </div>
                                                <span className="text-sm">{attendee.firstName} {attendee.lastName}</span>
                                                <button type="button" onClick={() => handleRemoveAttendee(attendee.id)} className="text-gray-400 hover:text-red-500">✕</button>
                                            </div>
                                        ))}
                                    </div>
                                ) : (
                                    <div className="text-center text-gray-400 text-sm py-2">Henüz katılımcı seçilmedi</div>
                                )}
                            </div>

                            <button type="button" onClick={() => setShowPersonelModal(true)} className="w-full py-2 border-2 border-dashed border-gray-300 rounded-lg text-gray-500 hover:border-indigo-500 hover:text-indigo-600 flex items-center justify-center gap-2">
                                + Katılımcı Ekle
                            </button>
                        </div>
                    </div>
                </div>

                <div className="flex justify-end gap-3">
                    <Link to="/meetings" className="px-4 py-2 border rounded-lg">İptal</Link>
                    <button type="submit" disabled={loading} className="px-5 py-2 bg-indigo-600 text-white rounded-lg">
                        {loading ? 'Güncelleniyor...' : 'Değişiklikleri Kaydet'}
                    </button>
                </div>
            </form>

            {/* Personel Modal - CreateMeeting'deki gibi */}
            {showPersonelModal && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-2xl max-h-[80vh] flex flex-col">
                        <div className="px-6 py-4 border-b flex justify-between items-center">
                            <h2 className="text-xl font-bold">Katılımcı Seç</h2>
                            <button onClick={() => setShowPersonelModal(false)} className="text-gray-500">✕</button>
                        </div>
                        <div className="p-4 border-b">
                            <input type="text" placeholder="Personel ara..." value={personelSearch} onChange={(e) => setPersonelSearch(e.target.value)} className="w-full px-4 py-2 bg-gray-100 rounded-lg" />
                        </div>
                        <div className="flex-1 overflow-y-auto p-4">
                            <div className="space-y-2">
                                {personels.filter(p => (p.firstName + ' ' + p.lastName + ' ' + p.email).toLowerCase().includes(personelSearch.toLowerCase())).map(personel => (
                                    <label key={personel.id} className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer ${selectedPersonelIds.includes(personel.id) ? 'bg-indigo-50 border border-indigo-200' : 'hover:bg-gray-50'}`}>
                                        <input type="checkbox" checked={selectedPersonelIds.includes(personel.id)} onChange={() => handleTogglePersonelSelection(personel.id)} />
                                        <div className="w-10 h-10 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-white">
                                            {personel.firstName?.charAt(0)}{personel.lastName?.charAt(0)}
                                        </div>
                                        <div>
                                            <p className="font-medium">{personel.firstName} {personel.lastName}</p>
                                            <p className="text-sm text-gray-500">{personel.email}</p>
                                        </div>
                                    </label>
                                ))}
                            </div>
                        </div>
                        <div className="px-6 py-4 border-t flex justify-between items-center">
                            <span>{selectedPersonelIds.length} kişi seçildi</span>
                            <div className="flex gap-2">
                                <button onClick={() => setShowPersonelModal(false)} className="px-4 py-2 border rounded-lg">İptal</button>
                                <button onClick={handleAddAttendees} className="px-4 py-2 bg-indigo-600 text-white rounded-lg">Ekle</button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {/* Müşteri Modal */}
            {showCustomerModal && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white rounded-xl shadow-xl w-full max-w-md max-h-[70vh] flex flex-col">
                        <div className="px-5 py-3 border-b flex justify-between">
                            <h2 className="text-lg font-bold">Müşteri Seç</h2>
                            <button onClick={() => setShowCustomerModal(false)} className="text-gray-500">✕</button>
                        </div>
                        <div className="p-3 border-b">
                            <input type="text" placeholder="Müşteri ara..." value={customerSearch} onChange={(e) => setCustomerSearch(e.target.value)} className="w-full px-3 py-2 bg-gray-100 rounded-lg" />
                        </div>
                        <div className="flex-1 overflow-y-auto p-3">
                            {customers.filter(c => (c.firstName + ' ' + c.lastName + ' ' + (c.companyName || '')).toLowerCase().includes(customerSearch.toLowerCase())).map(customer => (
                                <label key={customer.id} className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer ${selectedCustomerId === customer.id ? 'bg-amber-50 border border-amber-200' : 'hover:bg-gray-50'}`}>
                                    <input type="radio" name="customer" checked={selectedCustomerId === customer.id} onChange={() => setSelectedCustomerId(customer.id)} />
                                    <div className="w-10 h-10 rounded-full bg-gradient-to-br from-amber-500 to-orange-500 flex items-center justify-center text-white">
                                        {customer.firstName?.charAt(0) || 'M'}
                                    </div>
                                    <div>
                                        <p className="font-medium">{getCustomerDisplayName(customer)}</p>
                                        <p className="text-xs text-gray-500">{customer.email}</p>
                                    </div>
                                </label>
                            ))}
                        </div>
                        <div className="px-5 py-3 border-t flex justify-between">
                            <span>{selectedCustomerId ? '1 seçildi' : 'Seçilmedi'}</span>
                            <div className="flex gap-2">
                                <button onClick={() => setShowCustomerModal(false)} className="px-3 py-1.5 border rounded-lg">İptal</button>
                                <button onClick={handleSelectCustomer} disabled={!selectedCustomerId} className="px-3 py-1.5 bg-amber-600 text-white rounded-lg">Seç</button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {/* Lead Modal */}
            {showLeadModal && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white rounded-xl shadow-xl w-full max-w-md max-h-[70vh] flex flex-col">
                        <div className="px-5 py-3 border-b flex justify-between">
                            <h2 className="text-lg font-bold">Lead Seç</h2>
                            <button onClick={() => setShowLeadModal(false)} className="text-gray-500">✕</button>
                        </div>
                        <div className="p-3 border-b">
                            <input type="text" placeholder="Lead ara..." value={leadSearch} onChange={(e) => setLeadSearch(e.target.value)} className="w-full px-3 py-2 bg-gray-100 rounded-lg" />
                        </div>
                        <div className="flex-1 overflow-y-auto p-3">
                            {leads.filter(l => (l.companyName + ' ' + (l.contactName || '')).toLowerCase().includes(leadSearch.toLowerCase())).map(lead => (
                                <label key={lead.id} className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer ${selectedLeadId === lead.id ? 'bg-cyan-50 border border-cyan-200' : 'hover:bg-gray-50'}`}>
                                    <input type="radio" name="lead" checked={selectedLeadId === lead.id} onChange={() => setSelectedLeadId(lead.id)} />
                                    <div className="w-10 h-10 rounded-full bg-gradient-to-br from-cyan-500 to-blue-500 flex items-center justify-center text-white">
                                        {lead.companyName?.charAt(0) || 'L'}
                                    </div>
                                    <div>
                                        <p className="font-medium">{getLeadDisplayName(lead)}</p>
                                        <p className="text-xs text-gray-500">{lead.email}</p>
                                    </div>
                                </label>
                            ))}
                        </div>
                        <div className="px-5 py-3 border-t flex justify-between">
                            <span>{selectedLeadId ? '1 seçildi' : 'Seçilmedi'}</span>
                            <div className="flex gap-2">
                                <button onClick={() => setShowLeadModal(false)} className="px-3 py-1.5 border rounded-lg">İptal</button>
                                <button onClick={handleSelectLead} disabled={!selectedLeadId} className="px-3 py-1.5 bg-cyan-600 text-white rounded-lg">Seç</button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}