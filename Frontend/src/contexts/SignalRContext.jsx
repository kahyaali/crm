import React, { createContext, useContext, useEffect, useState } from 'react';
import signalRService from '../services/signalRService';
import { useAuth } from './AuthContext';
import toast from 'react-hot-toast';

const SignalRContext = createContext();

export const useSignalR = () => useContext(SignalRContext);

export const SignalRProvider = ({ children }) => {
  const { user } = useAuth();
  const [isConnected, setIsConnected] = useState(false);
  const [notifications, setNotifications] = useState([]);
  const [refreshSignal, setRefreshSignal] = useState(0);  

  useEffect(() => {
    if (user) {
      connectSignalR();
    }

    return () => {
      signalRService.disconnect();
    };
  }, [user]);

  const connectSignalR = async () => {
    try {
      await signalRService.connect();
      setIsConnected(true);
      console.log('✅ SignalR bağlantısı kuruldu');


   // =====  UPLOAD PROGRESS DİNLEYİCİSİ =====
    signalRService.connection.on('ReceiveProgress', (progressData) => {
      console.log('📊 SignalR Progress ALINDI:', progressData);
      
      //  GLOBAL EVENT FIRLAT (Personels.jsx dinleyecek)
      window.dispatchEvent(new CustomEvent('uploadProgress', { 
        detail: progressData 
      }));
    });


 //  REFRESH CAMPAIGNS DİNLEYİCİSİ 
      signalRService.connection.on('RefreshCampaigns', () => {
        console.log('🔄 Kampanyalar yenileniyor...');
        setRefreshSignal(prev => prev + 1);
      });

      //  REFRESH INVOICES DİNLEYİCİSİ 
      signalRService.connection.on('RefreshInvoices', () => {
        console.log('🔄 Faturalar yenileniyor...');
        setRefreshSignal(prev => prev + 1);
      });

      //  REFRESH QUOTES DİNLEYİCİSİ 
      signalRService.connection.on('RefreshQuotes', () => {
        console.log('🔄 Teklifler yenileniyor...');
        setRefreshSignal(prev => prev + 1);
      });

      //  REFRESH CONTRACTS DİNLEYİCİSİ 
      signalRService.connection.on('RefreshContracts', () => {
        console.log('🔄 Sözleşmeler yenileniyor...');
        setRefreshSignal(prev => prev + 1);
      });

      // Refresh Opportunities
signalRService.connection.on('RefreshOpportunities', () => {
  console.log('🔄 Fırsatlar yenileniyor...');
  setRefreshSignal(prev => prev + 1);
});


      // Bildirim dinleyici
      signalRService.onNotification((notification) => {
        console.log('📢 Yeni bildirim:', notification);
        
        
        if (notification.type === 'NewTicket' || 
            notification.type === 'TicketStatusChanged' || 
            notification.type === 'TicketDeleted' ||
            notification.type === 'NewComment' ||      
            notification.type === 'TicketUpdated' ||
            notification.type === 'NewLead' ||        
            notification.type === 'LeadUpdated' ||     
            notification.type === 'LeadConverted' ||   
            notification.type === 'LeadDeleted' ||
          notification.type === 'MeetingCreated' ||    
        notification.type === 'MeetingUpdated' ||    
        notification.type === 'MeetingDeleted' ||
        notification.type === 'InvoiceCreated' ||
            notification.type === 'InvoiceUpdated' ||
            notification.type === 'InvoiceDeleted' ||
            notification.type === 'QuoteCreated' ||
            notification.type === 'QuoteUpdated' ||
            notification.type === 'QuoteDeleted' ||
            notification.type === 'ContractCreated' ||
            notification.type === 'ContractUpdated' ||
            notification.type === 'ContractDeleted' ||
            notification.type === 'CampaignCreated' ||    
            notification.type === 'CampaignUpdated' ||    
            notification.type === 'CampaignDeleted' ||
            notification.type === 'OpportunityCreated' ||
    notification.type === 'OpportunityUpdated' ||
    notification.type === 'OpportunityDeleted' ||
    notification.type === 'OpportunityWon' ||
    notification.type === 'OpportunityLost'  
          ) {     
          console.log('🔄 Liste yenilenecek:', notification.type);
          setRefreshSignal(prev => prev + 1);
        }
        
        // Toast bildirimi göster
        toast.custom((t) => (
          <div className={`${t.visible ? 'animate-enter' : 'animate-leave'} max-w-md w-full bg-white dark:bg-gray-800 shadow-lg rounded-lg pointer-events-auto ring-1 ring-black ring-opacity-5`}>
            <div className="p-4">
              <div className="flex items-start">
                <div className="flex-shrink-0 pt-0.5">
                  <div className="h-10 w-10 rounded-full bg-indigo-100 dark:bg-indigo-900 flex items-center justify-center">
                    <span className="text-lg">🔔</span>
                  </div>
                </div>
                <div className="ml-3 flex-1">
                  <p className="text-sm font-medium text-gray-900 dark:text-white">
                    {notification.title}
                  </p>
                  <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                    {notification.message}
                  </p>
                  <p className="mt-1 text-xs text-gray-400">
                    {new Date(notification.timestamp).toLocaleTimeString('tr-TR')}
                  </p>
                </div>
              </div>
            </div>
          </div>
        ), { duration: 5000 });

        setNotifications(prev => [notification, ...prev].slice(0, 50));
      });

      // Kullanıcıya özel bildirim dinleyici
      signalRService.onUserNotification((userId, title, message) => {
        if (userId === user?.id) {
          toast.success(`${title}: ${message}`);
        }
      });

    } catch (error) {
      console.error('❌ SignalR bağlantı hatası:', error);
      setIsConnected(false);
    }
  };

  const clearNotifications = () => {
    setNotifications([]);
  };

  const value = {
    isConnected,
    refreshSignal,  
    notifications,
    clearNotifications,
    sendNotification: signalRService.sendNotification.bind(signalRService),
    sendNotificationToAll: signalRService.sendNotificationToAll.bind(signalRService)
  };

  return (
    <SignalRContext.Provider value={value}>
      {children}
    </SignalRContext.Provider>
  );
};