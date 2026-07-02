import api from './api';

const API_URL = import.meta.env.VITE_API_URL || 'https://localhost:7221';

const ticketApi = {
  getAll: async (params) => {
    const response = await api.get('/tickets', { params });
    return response.data;
  },

  getAllIncludingAll: async (params) => {
    const response = await api.get('/tickets/all', { params });
    return response.data;
  },

  getById: async (id) => {
    const response = await api.get(`/tickets/${id}`);
    return response.data;
  },

  create: async (data) => {
    const response = await api.post('/tickets', data);
    return response.data;
  },

  update: async (id, data) => {
    const response = await api.put(`/tickets/${id}`, data);
    return response.data;
  },

  delete: async (id) => {
    const response = await api.delete(`/tickets/${id}`);
    return response.data;
  },

  addComment: async (ticketId, data) => {
    const response = await api.post(`/tickets/${ticketId}/comments`, data);
    return response.data;
  },

  getStatusList: async () => {
    const response = await api.get('/tickets/status-list');
    return response.data;
  },

  getPriorityList: async () => {
    const response = await api.get('/tickets/priority-list');
    return response.data;
  },

  getCategoryList: async () => {
    const response = await api.get('/tickets/category-list');
    return response.data;
  },

  getStats: async () => {
    const response = await api.get('/tickets/stats');
    return response.data;
  }
};

export default ticketApi;