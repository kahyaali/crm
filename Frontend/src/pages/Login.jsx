import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { Link, useNavigate } from 'react-router-dom';
import api from '../services/api';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      await login(email, password);
      navigate('/dashboard');
    } catch (err) {
      setError('Email veya şifre hatalı');
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-[#0f172a] p-4 font-sans selection:bg-indigo-500/30">
      {/* Arka Plan Dekorasyonu */}
      <div className="fixed inset-0 pointer-events-none">
        <div className="absolute top-0 left-0 w-96 h-96 bg-indigo-500/20 rounded-full blur-[128px]"></div>
        <div className="absolute bottom-0 right-0 w-96 h-96 bg-purple-500/20 rounded-full blur-[128px]"></div>
      </div>

      {/* Login Kartı */}
      <div className="relative w-full max-w-4xl bg-white/5 backdrop-blur-2xl border border-white/10 rounded-3xl overflow-hidden shadow-2xl flex flex-col md:flex-row">
        
        {/* Sol Taraf - Marka/Bilgi */}
        <div className="hidden md:flex md:w-1/2 bg-gradient-to-br from-indigo-600 to-purple-700 p-12 flex-col justify-between text-white">
          <div>
            <h2 className="text-4xl font-bold mb-4">CRM Platformu</h2>
            <p className="text-indigo-100 text-lg">İş süreçlerinizi modernize edin, yönetimi tek noktadan ele alın.</p>
          </div>
          <div className="text-sm text-indigo-200">© 2026 CRM Sistemi v1.0</div>
        </div>

        {/* Sağ Taraf - Form */}
        <div className="w-full md:w-1/2 p-8 md:p-12 bg-white/5">
          <div className="mb-8">
            <h1 className="text-2xl font-bold text-white mb-1">Giriş Yap</h1>
            <p className="text-gray-400">Tekrar hoş geldiniz, bilgilerinizi girin.</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            {error && <div className="p-3 bg-red-500/10 border border-red-500/50 text-red-400 rounded-xl text-sm">{error}</div>}

            <div className="space-y-2">
              <label className="text-gray-300 text-xs font-semibold uppercase tracking-wider">Email</label>
              <input 
                type="email" 
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full bg-white/5 border border-white/10 rounded-xl p-3 text-white focus:ring-2 focus:ring-indigo-500 outline-none transition-all"
                placeholder="isim@sirket.com"
                required 
              />
            </div>

            <div className="space-y-2">
              <div className="flex justify-between">
                <label className="text-gray-300 text-xs font-semibold uppercase tracking-wider">Şifre</label>
                <Link to="/forgot-password" text-xs className="text-indigo-400 hover:text-indigo-300">Unuttun mu?</Link>
              </div>
              <input 
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full bg-white/5 border border-white/10 rounded-xl p-3 text-white focus:ring-2 focus:ring-indigo-500 outline-none transition-all"
                placeholder="••••••••"
                required 
              />
            </div>

            <button 
              type="submit" 
              disabled={loading}
              className="w-full bg-indigo-600 hover:bg-indigo-500 text-white font-bold py-3 rounded-xl transition-all shadow-lg shadow-indigo-500/20 active:scale-[0.98]"
            >
              {loading ? "Giriş Yapılıyor..." : "Giriş Yap"}
            </button>
          </form>

          <p className="mt-8 text-center text-gray-500 text-sm">
            Hesabınız yok mu? <Link to="/register" className="text-indigo-400 font-semibold hover:underline">Kaydolun</Link>
          </p>
        </div>
      </div>
    </div>
  );
}