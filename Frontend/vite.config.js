import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:7221',
        changeOrigin: true,
        secure: false,
        // API istekleri için timeout ayarı
        timeout: 60000,
      },
      // SignalR Hub için proxy - WebSocket desteği ile
      '/hubs': {
        target: 'https://localhost:7221',
        changeOrigin: true,
        secure: false,
        ws: true, // WebSocket desteğini aktifleştir
        timeout: 60000,
        // WebSocket için özel yapılandırma
        configure: (proxy, options) => {
          proxy.on('error', (err, req, res) => {
            console.log('proxy error', err);
          });
          proxy.on('proxyReq', (proxyReq, req, res) => {
            console.log('Proxying request:', req.url);
          });
          proxy.on('proxyRes', (proxyRes, req, res) => {
            console.log('Proxy response:', proxyRes.statusCode);
          });
        }
      }
    }
  },
  // Geliştirme ortamı için ek ayarlar
  optimizeDeps: {
    include: ['@microsoft/signalr']
  }
})