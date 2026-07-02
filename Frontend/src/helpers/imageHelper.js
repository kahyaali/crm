
// Backend'in gerçek adresi (vite.config.js'deki proxy target ile aynı)
const BACKEND_URL = import.meta.env.VITE_API_URL || 'https://localhost:7221';

export const getImageUrl = (path) => {
  if (!path) return null;
  
  // Zaten tam URL ise (http, https, blob, data)
  if (path.startsWith('http://') || 
      path.startsWith('https://') || 
      path.startsWith('blob:') || 
      path.startsWith('data:')) {
    return path;
  }
  
  // Göreceli yol ise backend URL'ini ekle
  if (path.startsWith('/')) {
    return `${BACKEND_URL}${path}`;
  }
  
  // / ile başlamıyorsa ekle
  return `${BACKEND_URL}/${path}`;
};