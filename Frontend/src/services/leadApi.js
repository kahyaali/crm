// src/services/leadApi.js
import api from './api';

const leadApi = {
  // Lead listesi
  getAll: async (params) => {
    const response = await api.get('/leads', { params });
    return response.data;
  },

  // Lead detayı
  getById: async (id) => {
    const response = await api.get(`/leads/${id}`);
    return response.data;
  },

  // Lead oluştur
  create: async (data) => {
    const response = await api.post('/leads', data);
    return response.data;
  },

  // Lead güncelle
  update: async (id, data) => {
    const response = await api.put(`/leads/${id}`, data);
    return response.data;
  },

  // Lead sil
  delete: async (id) => {
    const response = await api.delete(`/leads/${id}`);
    return response.data;
  },

  // Lead'i müşteriye dönüştür
  convertToCustomer: async (id, data) => {
    const response = await api.post(`/leads/${id}/convert-to-customer`, data);
    return response.data;
  },

  // Source listesi
  getSourceList: async () => {
    const response = await api.get('/leads/source-list');
    return response.data;
  },

  // Status listesi
  getStatusList: async () => {
    const response = await api.get('/leads/status-list');
    return response.data;
  }
};

export default leadApi;