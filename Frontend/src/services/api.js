// src/services/api.js
import axios from 'axios';
import { toast } from 'react-hot-toast';

const getToken = () => localStorage.getItem('accessToken');

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
  timeout: 30000, // 30 saniye timeout
});

// Refresh işleminin devam edip etmediğini takip eden değişken
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach((prom) => {
    if (error) prom.reject(error);
    else prom.resolve(token);
  });
  failedQueue = [];
};

api.interceptors.request.use((config) => {
  const token = getToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // ============ RATE LIMITING (429) ============
    if (error.response?.status === 429) {
      toast.error('Çok fazla istek! Lütfen 30 saniye bekleyin.');
      console.warn('⚠️ Rate limit aşıldı!');
      
      // Rate limit durumunda retry etme, direkt hata fırlat
      return Promise.reject(error);
    }

    // ============ UNAUTHORIZED (401) ============
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (originalRequest.url.includes('/Auth/login')) return Promise.reject(error);

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = localStorage.getItem('refreshToken');
      
      return new Promise((resolve, reject) => {
        axios.post('/api/Auth/refresh', { refreshToken }, { withCredentials: true })
          .then(({ data }) => {
            const { accessToken, refreshToken: newRefreshToken } = data;
            localStorage.setItem('accessToken', accessToken);
            if (newRefreshToken) localStorage.setItem('refreshToken', newRefreshToken);
            
            api.defaults.headers.common['Authorization'] = `Bearer ${accessToken}`;
            originalRequest.headers.Authorization = `Bearer ${accessToken}`;
            
            processQueue(null, accessToken);
            resolve(api(originalRequest));
          })
          .catch((err) => {
            processQueue(err, null);
            localStorage.clear();
            window.location.href = '/login';
            toast.error('Oturum süresi doldu, lütfen tekrar giriş yapın.');
            reject(err);
          })
          .finally(() => { isRefreshing = false; });
      });
    }

    // ============ SERVER ERROR (500+) ============
    if (error.response?.status >= 500) {
      toast.error('Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.');
    }

    // ============ TIMEOUT ============
    if (error.code === 'ECONNABORTED' || error.message?.includes('timeout')) {
      toast.error('İstek zaman aşımına uğradı. Lütfen tekrar deneyin.');
    }

    return Promise.reject(error);
  }
);

export default api;