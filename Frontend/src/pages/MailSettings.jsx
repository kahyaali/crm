import { useState, useEffect } from 'react';
import api from '../services/api';
import { 
  Server, 
  Hash, 
  User, 
  Key, 
  Mail, 
  Send, 
  Settings, 
  Save, 
  CheckCircle2, 
  AlertCircle 
} from 'lucide-react';

export default function MailSettings() {
  const [settings, setSettings] = useState({
    smtpServer: '',
    smtpPort: 587,
    smtpUsername: '',
    smtpPassword: '',
    fromEmail: '',
    fromName: '',
    enableSsl: true
  });
  const [testEmail, setTestEmail] = useState('');
  const [notification, setNotification] = useState({ message: '', type: '' });
  const [loading, setLoading] = useState(false);
  const [testLoading, setTestLoading] = useState(false);

  useEffect(() => {
    fetchSettings();
  }, []);

  const showNotification = (message, type = 'success') => {
    setNotification({ message, type });
    setTimeout(() => setNotification({ message: '', type: '' }), 4000);
  };

  const fetchSettings = async () => {
    try {
      const response = await api.get('/MailSettings');
      setSettings(response.data);
    } catch (error) {
      console.error('Ayarlar yüklenemedi:', error);
      showNotification('Ayarlar sunucudan yüklenemedi!', 'error');
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      await api.put('/MailSettings', settings);
      showNotification('Sistem mail ayarları başarıyla kaydedildi.', 'success');
    } catch (error) {
      showNotification('Hata: ' + (error.response?.data?.message || error.message), 'error');
    } finally {
      setLoading(false);
    }
  };

  const sendTestMail = async () => {
    if (!testEmail) {
      showNotification('Lütfen geçerli bir test e-posta adresi girin.', 'error');
      return;
    }
    setTestLoading(true);
    try {
      await api.post('/MailSettings/test', { email: testEmail });
      showNotification(`Test maili ${testEmail} adresine başarıyla gönderildi!`, 'success');
    } catch (error) {
      showNotification('Mail gönderimi başarısız: ' + (error.response?.data?.message || error.message), 'error');
    } finally {
      setTestLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950 p-6 sm:p-10 transition-colors duration-200">
      <div className="max-w-4xl mx-auto space-y-8">
        
        {/* Başlık Bölümü */}
        <div className="flex items-center gap-3 border-b border-gray-200 dark:border-gray-800 pb-5">
          <div className="p-3 bg-blue-600/10 text-blue-600 rounded-xl">
            <Settings className="w-7 h-7" />
          </div>
          <div>
            <h1 className="text-3xl font-extrabold text-gray-900 dark:text-white tracking-tight">Mail Ayarları</h1>
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">Sistem genelinde kullanılacak SMTP sunucu yapılandırması</p>
          </div>
        </div>

        {/* Canlı Bildirim Paneli */}
        {notification.message && (
          <div className={`flex items-center gap-3 p-4 rounded-xl border animate-in fade-in slide-in-from-top-4 duration-300 ${
            notification.type === 'success' 
              ? 'bg-emerald-50 dark:bg-emerald-950/30 border-emerald-200 dark:border-emerald-800 text-emerald-800 dark:text-emerald-400' 
              : 'bg-rose-50 dark:bg-rose-950/30 border-rose-200 dark:border-rose-800 text-rose-800 dark:text-rose-400'
          }`}>
            {notification.type === 'success' ? <CheckCircle2 className="w-5 h-5 flex-shrink-0" /> : <AlertCircle className="w-5 h-5 flex-shrink-0" />}
            <span className="text-sm font-medium">{notification.message}</span>
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          
          {/* Ana Ayarlar Formu */}
          <form onSubmit={handleSubmit} className="md:col-span-2 bg-white dark:bg-gray-900 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-800 p-6 space-y-6">
            <h2 className="text-lg font-semibold text-gray-800 dark:text-gray-200 border-b border-gray-100 dark:border-gray-800 pb-3">SMTP Sunucu Bilgileri</h2>
            
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-gray-600 dark:text-gray-400 uppercase tracking-wider">SMTP Server</label>
                <div className="relative">
                  <Server className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <input type="text" value={settings.smtpServer} onChange={(e) => setSettings({...settings, smtpServer: e.target.value})} className="w-full pl-10 pr-4 py-2.5 bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-sm dark:text-white transition-all" placeholder="smtp.gmail.com" required />
                </div>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-gray-600 dark:text-gray-400 uppercase tracking-wider">SMTP Port</label>
                <div className="relative">
                  <Hash className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <input type="number" value={settings.smtpPort} onChange={(e) => setSettings({...settings, smtpPort: parseInt(e.target.value)})} className="w-full pl-10 pr-4 py-2.5 bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-sm dark:text-white transition-all" placeholder="587" required />
                </div>
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-gray-600 dark:text-gray-400 uppercase tracking-wider">Kullanıcı Adı (Email)</label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <input type="text" value={settings.smtpUsername} onChange={(e) => setSettings({...settings, smtpUsername: e.target.value})} className="w-full pl-10 pr-4 py-2.5 bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-sm dark:text-white transition-all" placeholder="ornek@domain.com" required />
                </div>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-gray-600 dark:text-gray-400 uppercase tracking-wider">Şifre (App Password)</label>
                <div className="relative">
                  <Key className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <input type="password" value={settings.smtpPassword} onChange={(e) => setSettings({...settings, smtpPassword: e.target.value})} className="w-full pl-10 pr-4 py-2.5 bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-sm dark:text-white transition-all" placeholder="••••••••••••" required />
                </div>
              </div>
            </div>

            <h2 className="text-lg font-semibold text-gray-800 dark:text-gray-200 border-b border-gray-100 dark:border-gray-800 pb-3 pt-2">Kimlik Bilgileri</h2>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-gray-600 dark:text-gray-400 uppercase tracking-wider">Gönderen Email</label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <input type="email" value={settings.fromEmail} onChange={(e) => setSettings({...settings, fromEmail: e.target.value})} className="w-full pl-10 pr-4 py-2.5 bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-sm dark:text-white transition-all" placeholder="noreply@crm.com" required />
                </div>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-gray-600 dark:text-gray-400 uppercase tracking-wider">Gönderen İsim / Başlık</label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <input type="text" value={settings.fromName} onChange={(e) => setSettings({...settings, fromName: e.target.value})} className="w-full pl-10 pr-4 py-2.5 bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-sm dark:text-white transition-all" placeholder="CRM Sistemi" required />
                </div>
              </div>
            </div>

            {/* Modern Toggle Switch */}
            <div className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-800/30 rounded-xl border border-gray-100 dark:border-gray-800">
              <div>
                <span className="block text-sm font-semibold text-gray-800 dark:text-gray-200">SSL / TLS Güvenliği</span>
                <span className="text-xs text-gray-500 dark:text-gray-400">Veri iletimi sırasında şifreleme katmanını aktif eder.</span>
              </div>
              <label className="relative inline-flex items-center cursor-pointer select-none">
                <input type="checkbox" checked={settings.enableSsl} onChange={(e) => setSettings({...settings, enableSsl: e.target.checked})} className="sr-only peer" />
                <div className="w-11 h-6 bg-gray-200 dark:bg-gray-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
              </label>
            </div>

            <button type="submit" disabled={loading} className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-medium p-3 rounded-xl shadow-md shadow-blue-500/10 flex items-center justify-center gap-2 transition-all active:scale-[0.99]">
              <Save className="w-4 h-4" />
              {loading ? 'Kaydediliyor...' : 'Konfigürasyonu Kaydet'}
            </button>
          </form>

          {/* Sağ Kolon - Test Paneli */}
          <div className="space-y-6">
            <div className="bg-white dark:bg-gray-900 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-800 p-6">
              <h2 className="text-lg font-semibold text-gray-800 dark:text-gray-200 mb-2">Bağlantıyı Test Et</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">Ayarların doğruluğunu onaylamak için sisteme anlık bir test e-postası tetikleyin.</p>
              
              <div className="space-y-4">
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold text-gray-600 dark:text-gray-400 uppercase tracking-wider">Alıcı E-posta</label>
                  <div className="relative">
                    <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                    <input type="email" placeholder="deneme@gmail.com" value={testEmail} onChange={(e) => setTestEmail(e.target.value)} className="w-full pl-10 pr-4 py-2.5 bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700 rounded-xl focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 outline-none text-sm dark:text-white transition-all" />
                  </div>
                </div>

                <button onClick={sendTestMail} disabled={testLoading} className="w-full bg-emerald-600 hover:bg-emerald-700 disabled:bg-emerald-400 text-white font-medium p-3 rounded-xl shadow-md shadow-emerald-500/10 flex items-center justify-center gap-2 transition-all active:scale-[0.99]">
                  <Send className="w-4 h-4" />
                  {testLoading ? 'Gönderiliyor...' : 'Test Maili Gönder'}
                </button>
              </div>
            </div>

            {/* Küçük Yardım Kartı */}
            <div className="bg-amber-500/5 dark:bg-amber-500/5 border border-amber-500/20 rounded-2xl p-4">
              <h3 className="text-sm font-semibold text-amber-800 dark:text-amber-400 flex items-center gap-2 mb-1">
                <AlertCircle className="w-4 h-4" /> İpucu
              </h3>
              <p className="text-xs text-amber-700/80 dark:text-amber-400/70 leading-relaxed">
                Gmail veya Outlook kullanıyorsanız, kişisel şifreniz yerine hesap ayarlarınızdan <strong>Uygulama Şifresi (App Password)</strong> oluşturup onu girmeniz gerekir. Aksi takdirde sunucu bağlantıyı reddedecektir.
              </p>
            </div>
          </div>

        </div>

      </div>
    </div>
  );
}