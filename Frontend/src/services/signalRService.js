import * as signalR from '@microsoft/signalr';

class SignalRService {
  constructor() {
    this.connection = null;
    this.listeners = new Map();
  }

  async connect() {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    const token = localStorage.getItem('accessToken');
    
    // 🔥 Vite için import.meta.env kullan
    const apiUrl = import.meta.env.VITE_API_URL || 'https://localhost:7221';
    
    console.log('SignalR bağlanıyor:', `${apiUrl}/hubs/notification`);
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/hubs/notification`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.onreconnecting((error) => {
      console.warn('SignalR yeniden bağlanıyor:', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR yeniden bağlandı:', connectionId);
    });

    this.connection.onclose((error) => {
      console.error('SignalR bağlantısı kapandı:', error);
    });

    await this.connection.start();
    console.log('✅ SignalR bağlantısı kuruldu');
  }

  onNotification(callback) {
    if (!this.connection) return;
    this.connection.on('ReceiveNotification', callback);
  }

  onUserNotification(callback) {
    if (!this.connection) return;
    this.connection.on('SendNotificationToUser', callback);
  }

  async sendNotification(userId, title, message) {
    if (!this.connection) return;
    await this.connection.invoke('SendNotificationToUser', userId, title, message);
  }

  async sendNotificationToAll(title, message) {
    if (!this.connection) return;
    await this.connection.invoke('SendNotificationToAll', title, message);
  }

  disconnect() {
    if (this.connection) {
      this.connection.stop();
    }
  }
}

export default new SignalRService();