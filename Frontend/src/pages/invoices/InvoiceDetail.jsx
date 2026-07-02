// src/pages/invoices/InvoiceDetail.jsx
import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import api from '../../services/api';
import toast from 'react-hot-toast';
import Swal from 'sweetalert2';

export default function InvoiceDetail() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user } = useAuth();
    const [invoice, setInvoice] = useState(null);
    const [loading, setLoading] = useState(true);
    const [showPaymentModal, setShowPaymentModal] = useState(false);
    const [paymentAmount, setPaymentAmount] = useState('');
    const [paymentDate, setPaymentDate] = useState(new Date().toISOString().slice(0, 16));
    const [paymentMethod, setPaymentMethod] = useState('Nakit');
    const [paymentNotes, setPaymentNotes] = useState('');
    const [addingPayment, setAddingPayment] = useState(false);

    const isCreator = invoice?.createdByPersonelId === user?.personelId;
    const isAdmin = user?.role === 'SystemAdmin' || user?.role === 'Admin';
    const canEdit = isAdmin || isCreator;
    const remainingAmount = invoice ? invoice.totalAmount - invoice.paidAmount : 0;

    useEffect(() => {
        fetchInvoice();
    }, [id]);

    const fetchInvoice = async () => {
        try {
            setLoading(true);
            const response = await api.get(`/invoices/${id}`);
            setInvoice(response.data);
        } catch (error) {
            toast.error('Fatura detayı yüklenemedi');
            navigate('/invoices');
        } finally {
            setLoading(false);
        }
    };

    const handleAddPayment = async () => {
        if (!paymentAmount || Number(paymentAmount) <= 0) {
            toast.error('Geçerli bir ödeme tutarı giriniz');
            return;
        }

        if (Number(paymentAmount) > remainingAmount) {
            toast.error(`Ödeme tutarı kalan bakiye (${formatCurrency(remainingAmount)})'den fazla olamaz`);
            return;
        }

        setAddingPayment(true);
        try {
            await api.post(`/invoices/${id}/add-payment`, {
                invoiceId: Number(id),
                paymentDate: paymentDate,
                amount: Number(paymentAmount),
                paymentMethod: paymentMethod,
                notes: paymentNotes
            });
            
            toast.success('Ödeme başarıyla eklendi');
            setShowPaymentModal(false);
            setPaymentAmount('');
            setPaymentNotes('');
            fetchInvoice();
        } catch (error) {
            toast.error(error.response?.data?.message || 'Ödeme eklenemedi');
        } finally {
            setAddingPayment(false);
        }
    };

    const formatDate = (date) => {
        if (!date) return '-';
        return new Date(date).toLocaleString('tr-TR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    const formatCurrency = (amount) => {
        return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(amount || 0);
    };

    const getStatusBadge = (status) => {
        const config = {
            'Gönderildi': { icon: '📨', text: 'Gönderildi', color: 'bg-blue-100 text-blue-800' },
            'Kısmen Ödendi': { icon: '💰', text: 'Kısmen Ödendi', color: 'bg-yellow-100 text-yellow-800' },
            'Ödendi': { icon: '✅', text: 'Ödendi', color: 'bg-green-100 text-green-800' },
            'Gecikmiş': { icon: '⚠️', text: 'Gecikmiş', color: 'bg-red-100 text-red-800' },
            'İptal': { icon: '❌', text: 'İptal', color: 'bg-gray-100 text-gray-800' }
        };
        const c = config[status] || config['Gönderildi'];
        return <span className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium ${c.color}`}>{c.icon} {c.text}</span>;
    };

    const handlePrint = () => {
        window.print();
    };

    if (loading) {
        return (
            <div className="flex justify-center items-center h-96">
                <div className="animate-spin rounded-full h-12 w-12 border-4 border-indigo-500/20 border-t-indigo-600"></div>
            </div>
        );
    }

    if (!invoice) return null;

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-blue-50 py-8">
            <div className="max-w-6xl mx-auto px-6">
                {/* Header */}
                <div className="relative bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-2xl overflow-hidden mb-8">
                    <div className="relative px-6 py-8">
                        <div className="flex flex-wrap justify-between items-start gap-4">
                            <div>
                                <Link to="/invoices" className="inline-flex items-center gap-2 text-white/80 hover:text-white mb-4">
                                    ← Tüm Faturalar
                                </Link>
                                <h1 className="text-3xl font-bold">{invoice.invoiceNumber}</h1>
                                <div className="flex flex-wrap gap-3 mt-2">
                                    {getStatusBadge(invoice.status)}
                                    <span className="text-white/80 text-sm">
                                        Oluşturan: {invoice.createdByPersonelName || 'Sistem'} • {formatDate(invoice.createdAt)}
                                    </span>
                                </div>
                            </div>
                            <div className="flex gap-3">
                                {canEdit && invoice.status !== 'Ödendi' && invoice.status !== 'İptal' && (
                                    <Link to={`/invoices/edit/${invoice.id}`} className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 backdrop-blur-sm rounded-xl hover:bg-white/30">
                                        ✏️ Düzenle
                                    </Link>
                                )}
                                {remainingAmount > 0 && invoice.status !== 'İptal' && (
                                    <button onClick={() => setShowPaymentModal(true)} className="inline-flex items-center gap-2 px-4 py-2 bg-green-500/80 backdrop-blur-sm rounded-xl hover:bg-green-600">
                                        💰 Ödeme Al
                                    </button>
                                )}
                                <button onClick={handlePrint} className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 backdrop-blur-sm rounded-xl hover:bg-white/30">
                                    🖨️ Yazdır
                                </button>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Fatura Detayları */}
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Sol - Fatura Bilgileri */}
                    <div className="lg:col-span-2 space-y-6">
                        {/* Müşteri Bilgileri */}
                        <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                            <div className="px-6 py-4 bg-gradient-to-r from-amber-50 to-orange-50 border-b border-gray-100">
                                <h2 className="text-lg font-semibold flex items-center gap-2">
                                    <span className="text-xl">🏢</span> Müşteri Bilgileri
                                </h2>
                            </div>
                            <div className="p-6">
                                <p className="font-semibold text-gray-900">{invoice.customerName}</p>
                                {invoice.customerEmail && <p className="text-sm text-gray-500 mt-1">{invoice.customerEmail}</p>}
                                {invoice.customerPhone && <p className="text-sm text-gray-500">{invoice.customerPhone}</p>}
                            </div>
                        </div>

                        {/* Tarih Bilgileri */}
                        <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                            <div className="px-6 py-4 bg-gradient-to-r from-blue-50 to-indigo-50 border-b border-gray-100">
                                <h2 className="text-lg font-semibold flex items-center gap-2">
                                    <span className="text-xl">📅</span> Tarih Bilgileri
                                </h2>
                            </div>
                            <div className="p-6">
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <p className="text-xs text-gray-500">Fatura Tarihi</p>
                                        <p className="font-medium">{formatDate(invoice.invoiceDate)}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-gray-500">Son Ödeme Tarihi</p>
                                        <p className="font-medium text-orange-600">{formatDate(invoice.dueDate)}</p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Fatura Kalemleri */}
                        <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                            <div className="px-6 py-4 bg-gradient-to-r from-gray-50 to-slate-50 border-b border-gray-100">
                                <h2 className="text-lg font-semibold flex items-center gap-2">
                                    <span className="text-xl">📦</span> Fatura Kalemleri
                                </h2>
                            </div>
                            <div className="overflow-x-auto">
                                <table className="w-full">
                                    <thead className="bg-gray-50">
                                        <tr>
                                            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500">Ürün</th>
                                            <th className="px-6 py-3 text-center text-xs font-semibold text-gray-500">Adet</th>
                                            <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500">Birim Fiyat</th>
                                            <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500">Toplam</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-gray-100">
                                        {invoice.items?.map((item, idx) => (
                                            <tr key={idx} className="hover:bg-gray-50">
                                                <td className="px-6 py-4">
                                                    <div className="font-medium text-gray-900">{item.productName}</div>
                                                    <div className="text-xs text-gray-400">{item.productCode}</div>
                                                </td>
                                                <td className="px-6 py-4 text-center">{item.quantity}</td>
                                                <td className="px-6 py-4 text-right">{formatCurrency(item.unitPrice)}</td>
                                                <td className="px-6 py-4 text-right font-medium">{formatCurrency(item.totalPrice)}</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                    <tfoot className="border-t border-gray-200 bg-gray-50">
                                        <tr>
                                            <td colSpan={3} className="px-6 py-3 text-right font-medium">Ara Toplam:</td>
                                            <td className="px-6 py-3 text-right">{formatCurrency(invoice.subTotal)}</td>
                                        </tr>
                                        <tr>
                                            <td colSpan={3} className="px-6 py-3 text-right font-medium">KDV (%{invoice.taxRate}):</td>
                                            <td className="px-6 py-3 text-right">{formatCurrency(invoice.taxAmount)}</td>
                                        </tr>
                                        <tr className="border-t">
                                            <td colSpan={3} className="px-6 py-3 text-right font-bold text-lg">Genel Toplam:</td>
                                            <td className="px-6 py-3 text-right font-bold text-lg text-indigo-600">{formatCurrency(invoice.totalAmount)}</td>
                                        </tr>
                                    </tfoot>
                                </table>
                            </div>
                        </div>

                        {/* Notlar */}
                        {invoice.notes && (
                            <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-gray-50 to-slate-50 border-b border-gray-100">
                                    <h2 className="text-lg font-semibold flex items-center gap-2">
                                        <span className="text-xl">📝</span> Notlar
                                    </h2>
                                </div>
                                <div className="p-6">
                                    <p className="text-gray-700 whitespace-pre-wrap">{invoice.notes}</p>
                                </div>
                            </div>
                        )}
                    </div>

                    {/* Sağ - Ödeme Bilgileri */}
                    <div className="lg:col-span-1 space-y-6">
                        {/* Özet */}
                        <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden sticky top-6">
                            <div className="px-6 py-4 bg-gradient-to-r from-green-50 to-emerald-50 border-b border-gray-100">
                                <h2 className="text-lg font-semibold flex items-center gap-2">
                                    <span className="text-xl">💰</span> Özet
                                </h2>
                            </div>
                            <div className="p-6 space-y-4">
                                <div className="flex justify-between items-center pb-2 border-b">
                                    <span className="text-gray-600">Toplam Tutar:</span>
                                    <span className="font-bold text-lg">{formatCurrency(invoice.totalAmount)}</span>
                                </div>
                                <div className="flex justify-between items-center pb-2 border-b">
                                    <span className="text-gray-600">Ödenen Tutar:</span>
                                    <span className="font-medium text-green-600">{formatCurrency(invoice.paidAmount)}</span>
                                </div>
                                <div className="flex justify-between items-center pt-2">
                                    <span className="text-gray-600">Kalan Tutar:</span>
                                    <span className={`font-bold text-lg ${remainingAmount > 0 ? 'text-red-600' : 'text-green-600'}`}>
                                        {formatCurrency(remainingAmount)}
                                    </span>
                                </div>
                                <div className="mt-4 pt-3 border-t">
                                    <div className="flex justify-between items-center">
                                        <span className="text-gray-600">Durum:</span>
                                        {getStatusBadge(invoice.status)}
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Ödeme Geçmişi */}
                        {invoice.payments?.length > 0 && (
                            <div className="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
                                <div className="px-6 py-4 bg-gradient-to-r from-gray-50 to-slate-50 border-b border-gray-100">
                                    <h2 className="text-lg font-semibold flex items-center gap-2">
                                        <span className="text-xl">💳</span> Ödeme Geçmişi
                                    </h2>
                                </div>
                                <div className="divide-y divide-gray-100">
                                    {invoice.payments.map((payment) => (
                                        <div key={payment.id} className="p-4">
                                            <div className="flex justify-between items-start">
                                                <div>
                                                    <p className="font-medium text-gray-900">{payment.paymentNumber}</p>
                                                    <p className="text-xs text-gray-400">{formatDate(payment.paymentDate)}</p>
                                                </div>
                                                <div className="text-right">
                                                    <p className="font-bold text-green-600">{formatCurrency(payment.amount)}</p>
                                                    <p className="text-xs text-gray-400">{payment.paymentMethod}</p>
                                                </div>
                                            </div>
                                            {payment.notes && <p className="text-xs text-gray-500 mt-1">{payment.notes}</p>}
                                            {payment.receivedByPersonelName && (
                                                <p className="text-xs text-gray-400 mt-1">Alan: {payment.receivedByPersonelName}</p>
                                            )}
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            {/* Ödeme Ekleme Modalı */}
            {showPaymentModal && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
                    <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
                        <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
                            <h2 className="text-xl font-bold">Ödeme Ekle</h2>
                            <button onClick={() => setShowPaymentModal(false)} className="text-gray-400 hover:text-gray-600">✕</button>
                        </div>
                        <div className="p-6 space-y-4">
                            <div>
                                <label className="text-sm font-medium text-gray-700 block mb-1">Kalan Bakiye</label>
                                <p className="text-2xl font-bold text-red-600">{formatCurrency(remainingAmount)}</p>
                            </div>
                            <div>
                                <label className="text-sm font-medium text-gray-700 block mb-1">Ödeme Tutarı *</label>
                                <input 
                                    type="number" 
                                    value={paymentAmount} 
                                    onChange={(e) => setPaymentAmount(e.target.value)} 
                                    className="w-full px-3 py-2 border rounded-lg"
                                    placeholder="0.00"
                                    step="0.01"
                                    autoFocus
                                />
                            </div>
                            <div>
                                <label className="text-sm font-medium text-gray-700 block mb-1">Ödeme Tarihi</label>
                                <input 
                                    type="datetime-local" 
                                    value={paymentDate} 
                                    onChange={(e) => setPaymentDate(e.target.value)} 
                                    className="w-full px-3 py-2 border rounded-lg"
                                />
                            </div>
                            <div>
                                <label className="text-sm font-medium text-gray-700 block mb-1">Ödeme Yöntemi</label>
                                <select 
                                    value={paymentMethod} 
                                    onChange={(e) => setPaymentMethod(e.target.value)} 
                                    className="w-full px-3 py-2 border rounded-lg"
                                >
                                    <option value="Nakit">💵 Nakit</option>
                                    <option value="Kredi Kartı">💳 Kredi Kartı</option>
                                    <option value="Banka Havalesi">🏦 Banka Havalesi</option>
                                    <option value="Çek">📄 Çek</option>
                                </select>
                            </div>
                            <div>
                                <label className="text-sm font-medium text-gray-700 block mb-1">Not</label>
                                <textarea 
                                    value={paymentNotes} 
                                    onChange={(e) => setPaymentNotes(e.target.value)} 
                                    className="w-full px-3 py-2 border rounded-lg"
                                    rows="2"
                                    placeholder="İsteğe bağlı..."
                                />
                            </div>
                        </div>
                        <div className="px-6 py-4 border-t border-gray-200 flex justify-end gap-3">
                            <button onClick={() => setShowPaymentModal(false)} className="px-4 py-2 border rounded-lg">İptal</button>
                            <button onClick={handleAddPayment} disabled={addingPayment} className="px-4 py-2 bg-indigo-600 text-white rounded-lg">
                                {addingPayment ? 'İşleniyor...' : 'Ödemeyi Ekle'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}