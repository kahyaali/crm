import api from './api';

const logService = {
    // Action Logs
    getActionLogs: (params) => {
        return api.get('/Logs/actions', { params });
    },
    
    // Error Logs
    getErrorLogs: (params) => {
        return api.get('/Logs/errors', { params });
    },
    
    // Soft Delete Action Logs
    softDeleteActionLogs: (ids) => {
        return api.post('/Logs/actions/soft-delete', { ids });
    },
    
    // Hard Delete Action Logs
    hardDeleteActionLogs: (ids) => {
        return api.post('/Logs/actions/hard-delete', { ids });
    },
    
    // Soft Delete Error Logs
    softDeleteErrorLogs: (ids) => {
        return api.post('/Logs/errors/soft-delete', { ids });
    },
    
    // Hard Delete Error Logs
    hardDeleteErrorLogs: (ids) => {
        return api.post('/Logs/errors/hard-delete', { ids });
    },
    
    // Resolve Error
    resolveError: (id, resolutionNote) => {
        return api.post(`/Logs/errors/${id}/resolve`, { resolutionNote });
    },
    
    // Delete All
    softDeleteAllActions: () => {
        return api.delete('/Logs/actions/all/soft');
    },
    
    hardDeleteAllActions: () => {
        return api.delete('/Logs/actions/all/hard');
    },
    
    softDeleteAllErrors: () => {
        return api.delete('/Logs/errors/all/soft');
    },
    
    hardDeleteAllErrors: () => {
        return api.delete('/Logs/errors/all/hard');
    }
};

export default logService;