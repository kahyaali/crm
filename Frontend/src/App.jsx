import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { SignalRProvider } from './contexts/SignalRContext';  
import MainLayout from './layouts/MainLayout';
import Login from './pages/Login';
import Register from './pages/Register';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';
import Dashboard from './pages/Dashboard';
import Customers from './pages/Customers';
import Personels from './pages/Personels';
import Products from './pages/Products';
import ProductCategories from './pages/ProductCategories';
import Brands from './pages/Brands';
import MailSettings from './pages/MailSettings';
import Users from './pages/Users';
import AdminRoute from './components/AdminRoute';
import Departments from './pages/Departments';
import Positions from './pages/Positions';
import Roles from './pages/Roles';
import PersonelDashboard from './pages/PersonelDashboard';
import Profile from './pages/Profile';
import ActionLogs from './pages/Logs/ActionLogs';
import ErrorLogs from './pages/Logs/ErrorLogs';
import MyTeam from './pages/MyTeam';
import MyCustomers from './pages/MyCustomers';
import PersonelDetail from './pages/PersonelDetail';
import CustomerDetail from './pages/CustomerDetail';
import ProductDetail from './pages/ProductDetail'; 
import Orders from './pages/Orders';
import OrderDetail from './pages/OrderDetail';
import CreateOrder from './pages/CreateOrder';
import EditOrder from './pages/EditOrder';
import ExchangeRateSettings from './pages/ExchangeRateSettings';
import Tickets from './pages/tickets/Tickets';
import TicketDetail from './pages/tickets/TicketDetail';
import CreateTicket from './pages/tickets/CreateTicket';
import EditTicket from './pages/tickets/EditTicket';
import Leads from './pages/leads/Leads';
import LeadDetail from './pages/leads/LeadDetail';
import CreateLead from './pages/leads/CreateLead';
import EditLead from './pages/leads/EditLead';
import NotificationsPage from './pages/NotificationsPage';
import Meetings from './pages/meetings/Meetings';
import CreateMeeting from './pages/meetings/CreateMeeting';
import MeetingDetail from './pages/meetings/MeetingDetail';
import EditMeeting from './pages/meetings/EditMeeting';
import Invoices from './pages/invoices/Invoices';
import CreateInvoice from './pages/invoices/CreateInvoice';
import EditInvoice from './pages/invoices/EditInvoice';
import InvoiceDetail from './pages/invoices/InvoiceDetail';
import Quotes from './pages/quotes/Quotes';
import CreateQuote from './pages/quotes/CreateQuote';
import EditQuote from './pages/quotes/EditQuote';
import QuoteDetail from './pages/quotes/QuoteDetail';
import Contracts from './pages/contracts/Contracts';
import CreateContract from './pages/contracts/CreateContract';
import EditContract from './pages/contracts/EditContract';
import ContractDetail from './pages/contracts/ContractDetail';
import Campaigns from './pages/campaigns/Campaigns';
import CreateCampaign from './pages/campaigns/CreateCampaign';
import EditCampaign from './pages/campaigns/EditCampaign';
import CampaignDetail from './pages/campaigns/CampaignDetail';
import Opportunities from './pages/opportunities/Opportunities';
import CreateOpportunity from './pages/opportunities/CreateOpportunity';
import EditOpportunity from './pages/opportunities/EditOpportunity';
import OpportunityDetail from './pages/opportunities/OpportunityDetail';
import Tasks from './pages/tasks/Tasks';
import CreateTask from './pages/tasks/CreateTask';
import EditTask from './pages/tasks/EditTask';
import TaskDetail from './pages/tasks/TaskDetail';
import ReportPage from './pages/ReportPage';


function PrivateRoute({ children }) {
  const { user, loading } = useAuth();
  if (loading) return <div className="flex items-center justify-center min-h-screen">Yükleniyor...</div>;
  return user ? children : <Navigate to="/login" />;
}

// Role göre dashboard seçen özel bileşen
function DashboardRouter() {
  const { user } = useAuth();
  
  if (user?.role === 'SystemAdmin' || user?.role === 'Admin') {
    return <Dashboard />;
  }
  return <PersonelDashboard />;
}

function AppRoutes() {
  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/forgot-password" element={<ForgotPassword />} />
      <Route path="/reset-password" element={<ResetPassword />} />
      
      {/* Private Routes */}
      <Route path="/" element={<PrivateRoute><MainLayout /></PrivateRoute>}>
        <Route index element={<Navigate to="/dashboard" />} />
        
        {/* Ana Sayfalar */}
        <Route path="dashboard" element={<DashboardRouter />} />
        <Route path="profile" element={<Profile />} />
        <Route path="my-team" element={<MyTeam />} />
        <Route path="my-customers" element={<MyCustomers />} />
        
        {/* Detay Sayfaları */}
        <Route path="personel-detail/:id" element={<PersonelDetail />} />
        <Route path="customer-detail/:id" element={<CustomerDetail />} />
        
        {/* Müşteri Yönetimi */}
        <Route path="customers" element={<Customers />} />
        
        {/* Personel Yönetimi (Admin) */}
        <Route path="personels" element={<Personels />} />
        <Route path="departments" element={<Departments />} />
        <Route path="positions" element={<Positions />} />
        
        {/* Ürün Yönetimi (Admin) */}
        <Route path="products" element={<Products />} />
        <Route path="/products/:id" element={<ProductDetail />} />
        <Route path="product-categories" element={<ProductCategories />} />
        <Route path="brands" element={<Brands />} />

        {/* Sipariş Yönetimi (Admin) */}
        <Route path="/orders/create" element={<CreateOrder />} />     
        <Route path="/orders/edit/:id" element={<EditOrder />} />   
        <Route path="/orders/:id" element={<OrderDetail />} />        
        <Route path="/orders" element={<Orders />} /> 

        {/* Ticket Yönetimi */} 
        <Route path="/tickets" element={<Tickets />} />
        <Route path="/tickets/create" element={<CreateTicket />} />
        <Route path="/tickets/:id" element={<TicketDetail />} />
        <Route path="/tickets/edit/:id" element={<EditTicket />} />

          {/* Lead Yönetimi */}
        <Route path="/leads" element={<Leads />} />
        <Route path="/leads/create" element={<CreateLead />} />
        <Route path="/leads/:id" element={<LeadDetail />} />
       <Route path="/leads/edit/:id" element={<EditLead />} />

         {/* Meeting  Yönetimi */}
       <Route path="/meetings" element={<Meetings />} />
       <Route path="/meetings/create" element={<CreateMeeting />} />
       <Route path="/meetings/:id" element={<MeetingDetail />} />
      <Route path="/meetings/edit/:id" element={<EditMeeting />} />

            {/* Invoice - Fatura  Yönetimi */}
      <Route path="/invoices" element={<Invoices />} />
      <Route path="/invoices/create" element={<CreateInvoice />} />
      <Route path="/invoices/edit/:id" element={<EditInvoice />} />
       <Route path="/invoices/:id" element={<InvoiceDetail />} />

            {/* Quote - Teklif  Yönetimi */}
        <Route path="/quotes" element={<Quotes />} />
       <Route path="/quotes/create" element={<CreateQuote />} />
       <Route path="/quotes/edit/:id" element={<EditQuote />} />
       <Route path="/quotes/:id" element={<QuoteDetail />} />

       {/* Contracts - Sözleşmeler  Yönetimi */}
       <Route path="/contracts" element={<Contracts />} />
       <Route path="/contracts/create" element={<CreateContract />} />
       <Route path="/contracts/edit/:id" element={<EditContract />} />
       <Route path="/contracts/:id" element={<ContractDetail />} />


       {/* Campaigns - Kamopanyalar  Yönetimi */}
       <Route path="/campaigns" element={<Campaigns />} />
       <Route path="/campaigns/create" element={<CreateCampaign />} />
       <Route path="/campaigns/edit/:id" element={<EditCampaign />} />
       <Route path="/campaigns/:id" element={<CampaignDetail />} />


        {/* Opportunities - Fırsatlar  Yönetimi */}
          <Route path="/opportunities" element={<Opportunities />} />
          <Route path="/opportunities/create" element={<CreateOpportunity />} />
          <Route path="/opportunities/edit/:id" element={<EditOpportunity />} />
          <Route path="/opportunities/:id" element={<OpportunityDetail />} />

            {/* Task - Görevler  Yönetimi */}
          <Route path="/tasks" element={<Tasks />} />
          <Route path="/tasks/create" element={<CreateTask />} />
          <Route path="/tasks/edit/:id" element={<EditTask />} />
          <Route path="/tasks/:id" element={<TaskDetail />} />

        {/* Notification Yönetimi */}
       <Route path="/notifications" element={<NotificationsPage />} />

       {/* Rapor Yönetimi */}
       <Route path="/reports" element={<ReportPage />} />
        
        {/* Yetki Yönetimi (Admin) */}
        <Route path="roles" element={<Roles />} />
        <Route path="users" element={<Users />} />
        
        {/* Sistem Ayarları (Admin) */}
        <Route path="mail-settings" element={<MailSettings />} />
        
        {/* Log Yönetimi (Admin) */}
        <Route path="logs/actions" element={<ActionLogs />} />
        <Route path="logs/errors" element={<ErrorLogs />} />

        <Route path="/exchange-rate-settings" element={<ExchangeRateSettings />} />
      </Route>
    </Routes>
  );
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <SignalRProvider>  
          <Toaster position="top-right" />
          <AppRoutes />
        </SignalRProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;