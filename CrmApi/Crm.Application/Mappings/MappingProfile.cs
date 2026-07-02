using AutoMapper;
using Crm.Application.DTOs.ActionLogs;
using Crm.Application.DTOs.Brand;
using Crm.Application.DTOs.Campaign;
using Crm.Application.DTOs.Contract;
using Crm.Application.DTOs.Customer;
using Crm.Application.DTOs.Department;
using Crm.Application.DTOs.ErrorLogs;
using Crm.Application.DTOs.Invoice;
using Crm.Application.DTOs.Lead;
using Crm.Application.DTOs.Meeting;
using Crm.Application.DTOs.Notification;
using Crm.Application.DTOs.Opportunity;
using Crm.Application.DTOs.Order;
using Crm.Application.DTOs.Payment;
using Crm.Application.DTOs.Personel;
using Crm.Application.DTOs.Position;
using Crm.Application.DTOs.Product;
using Crm.Application.DTOs.ProductCategory;
using Crm.Application.DTOs.Quote;
using Crm.Application.DTOs.Task;
using Crm.Application.DTOs.Ticket;
using Crm.Application.DTOs.User;
using Crm.Domain.Entities;

namespace Crm.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserDto>();
            CreateMap<CreateUserDto, User>();
            CreateMap<UpdateUserDto, User>();

            // Customer mappings
            CreateMap<Customer, CustomerDto>()
                .ForMember(dest => dest.AssignedToPersonelName,
               opt => opt.MapFrom(src => src.AssignedToPersonel != null ? $"{src.AssignedToPersonel.FirstName} {src.AssignedToPersonel.LastName}" : null));
            CreateMap<CreateCustomerDto, Customer>();
            CreateMap<UpdateCustomerDto, Customer>();

            // Personel mappings
            CreateMap<Personel, PersonelDto>()
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency ?? "TRY"))
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
            .ForMember(dest => dest.PositionName, opt => opt.MapFrom(src => src.Position != null ? src.Position.Name : null))
            .ForMember(dest => dest.ManagerName, opt => opt.MapFrom(src => src.Manager != null ? $"{src.Manager.FirstName} {src.Manager.LastName}" : null));

            CreateMap<CreatePersonelDto, Personel>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            CreateMap<UpdatePersonelDto, Personel>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            //  Product mappings
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.BrandName,  
                    opt => opt.MapFrom(src => src.Brand != null ? src.Brand.Name : null))
                .ForMember(dest => dest.Currency,
                    opt => opt.MapFrom(src => src.Currency ?? "TRY"));

            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Brand, opt => opt.Ignore());  

            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Brand, opt => opt.Ignore())  
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

      

            // Department mappings
            CreateMap<Department, DepartmentDto>();
            CreateMap<CreateDepartmentDto, Department>();
            CreateMap<UpdateDepartmentDto, Department>();

            // Pozisyon mappings
            CreateMap<Position, PositionResponseDto>();
            CreateMap<CreatePositionDto, Position>();
            CreateMap<UpdatePositionDto, Position>();

            // Product Category mappings
            CreateMap<ProductCategory, ProductCategoryDto>()
                .ForMember(dest => dest.ParentCategoryName,
                    opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null));
            CreateMap<CreateProductCategoryDto, ProductCategory>();
            CreateMap<UpdateProductCategoryDto, ProductCategory>();

            // Brand mapping
            CreateMap<Brand, BrandDto>();
            CreateMap<CreateBrandDto, Brand>();
            CreateMap<UpdateBrandDto, Brand>();


            // ========== ACTION LOG MAPPINGS ==========
            CreateMap<ActionLog, ActionLogDto>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.PersonelName,
                    opt => opt.MapFrom(src => src.Personel != null ? $"{src.Personel.FirstName} {src.Personel.LastName}" : null));

            CreateMap<CreateActionLogDto, ActionLog>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.PersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.IpAddress, opt => opt.Ignore())
                .ForMember(dest => dest.UserAgent, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // ========== ERROR LOG MAPPINGS ==========
            CreateMap<ErrorLog, ErrorLogDto>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User != null ? src.User.Email : null));

            CreateMap<CreateErrorLogDto, ErrorLog>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.IpAddress, opt => opt.Ignore())
                .ForMember(dest => dest.IsResolved, opt => opt.Ignore())
                .ForMember(dest => dest.ResolvedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ResolutionNote, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // ========== ORDER MAPPINGS ==========

            // Order -> OrderDto
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}" : null))
                .ForMember(dest => dest.QuoteNumber,
                    opt => opt.MapFrom(src => src.Quote != null ? src.Quote.QuoteNumber : null))
                .ForMember(dest => dest.Items,
                    opt => opt.MapFrom(src => src.Items));

            // OrderItem -> OrderItemDto
            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.ProductSku,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Sku : null))
                .ForMember(dest => dest.TotalPrice,
                    opt => opt.MapFrom(src => src.Quantity * src.UnitPrice));

            // CreateOrderDto -> Order (OrderNumber ve hesaplamalar sonradan yapılacak)
            CreateMap<CreateOrderDto, Order>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OrderNumber, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
                .ForMember(dest => dest.TaxAmount, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore()); // Items ayrı işlenecek

            // UpdateOrderDto -> Order
            CreateMap<UpdateOrderDto, Order>()
                .ForMember(dest => dest.OrderNumber, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
                .ForMember(dest => dest.TaxAmount, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // CreateOrderItemDto -> OrderItem
            CreateMap<CreateOrderItemDto, OrderItem>()
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()); // Quantity * UnitPrice ile hesaplanacak

            // UpdateOrderItemDto -> OrderItem
            CreateMap<UpdateOrderItemDto, OrderItem>()
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            // ========== TICKET MAPPINGS ==========

            // Ticket -> TicketDto
            CreateMap<Ticket, TicketDto>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}" : null))
                .ForMember(dest => dest.AssignedToPersonelName,
                    opt => opt.MapFrom(src => src.AssignedToPersonel != null ? $"{src.AssignedToPersonel.FirstName} {src.AssignedToPersonel.LastName}" : null))
                .ForMember(dest => dest.CommentCount,
                    opt => opt.MapFrom(src => src.Comments != null ? src.Comments.Count : 0));

            // Ticket -> TicketDetailDto
            CreateMap<Ticket, TicketDetailDto>()
                .IncludeBase<Ticket, TicketDto>()
                .ForMember(dest => dest.CreatedByPersonelName,
                    opt => opt.MapFrom(src => src.CreatedByPersonel != null ? $"{src.CreatedByPersonel.FirstName} {src.CreatedByPersonel.LastName}" : null))
                .ForMember(dest => dest.Comments,
                    opt => opt.MapFrom(src => src.Comments != null ? src.Comments.OrderByDescending(c => c.CreatedAt).ToList() : new List<TicketComment>()));

            // CreateTicketDto -> Ticket
            CreateMap<CreateTicketDto, Ticket>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TicketNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ResolvedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ClosedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonel, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedToPersonel, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore());

            // UpdateTicketDto -> Ticket
            CreateMap<UpdateTicketDto, Ticket>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.TicketNumber, opt => opt.Ignore())
                .ForMember(dest => dest.ResolvedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ClosedAt, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // TicketComment mappings
            CreateMap<TicketComment, TicketCommentDto>()
                .ForMember(dest => dest.PersonelName,
                    opt => opt.MapFrom(src => src.Personel != null ? $"{src.Personel.FirstName} {src.Personel.LastName}" : null));

            CreateMap<CreateTicketCommentDto, TicketComment>()
                 .ForMember(dest => dest.Id, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.PersonelId, opt => opt.Ignore())
                 .ForMember(dest => dest.Personel, opt => opt.Ignore())
                 .ForMember(dest => dest.Ticket, opt => opt.Ignore());


            // ========== LEAD MAPPINGS ==========
            CreateMap<Lead, LeadDto>()
                .ForMember(dest => dest.AssignedToPersonelName,
                    opt => opt.MapFrom(src => src.AssignedToPersonel != null ? $"{src.AssignedToPersonel.FirstName} {src.AssignedToPersonel.LastName}" : null))
                .ForMember(dest => dest.ConvertedToCustomerName,
                    opt => opt.MapFrom(src => src.ConvertedToCustomer != null ? $"{src.ConvertedToCustomer.FirstName} {src.ConvertedToCustomer.LastName}" : null))
                .ForMember(dest => dest.CampaignName, opt => opt.MapFrom(src => src.Campaign != null ? src.Campaign.Name : null));

            CreateMap<CreateLeadDto, Lead>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ConvertedToCustomerId, opt => opt.Ignore())
                .ForMember(dest => dest.ConvertedToCustomer, opt => opt.Ignore());

            CreateMap<UpdateLeadDto, Lead>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.ConvertedToCustomerId, opt => opt.Ignore())
                .ForMember(dest => dest.ConvertedToCustomer, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<ConvertLeadToCustomerDto, Customer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FirstName, opt => opt.Ignore())
                .ForMember(dest => dest.LastName, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.Phone, opt => opt.Ignore())
                .ForMember(dest => dest.AccountNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());


            // Notification mappings
            CreateMap<Notification, NotificationDto>()
                .ForMember(dest => dest.PersonelName,
                    opt => opt.MapFrom(src => src.Personel != null ? $"{src.Personel.FirstName} {src.Personel.LastName}" : "Sistem"));
            CreateMap<CreateNotificationDto, Notification>();


            // ========== MEETING MAPPINGS ==========
            CreateMap<Meeting, MeetingDto>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}" : null))
                .ForMember(dest => dest.LeadName,
                    opt => opt.MapFrom(src => src.Lead != null ? $"{src.Lead.CompanyName} - {src.Lead.ContactName}" : null))
                .ForMember(dest => dest.Attendees,
                    opt => opt.MapFrom(src => src.Attendees))
            
                .ForMember(dest => dest.CreatedByPersonelId,
                    opt => opt.MapFrom(src => src.CreatedByPersonelId))
                .ForMember(dest => dest.CreatedByPersonelName,
                    opt => opt.MapFrom(src => src.CreatedByPersonel != null ? $"{src.CreatedByPersonel.FirstName} {src.CreatedByPersonel.LastName}" : null));

            CreateMap<MeetingAttendee, MeetingAttendeeDto>()
                .ForMember(dest => dest.PersonelId,
                    opt => opt.MapFrom(src => src.PersonelId))
                .ForMember(dest => dest.PersonelName,
                    opt => opt.MapFrom(src => src.Personel != null ? $"{src.Personel.FirstName} {src.Personel.LastName}" : null))
                .ForMember(dest => dest.AttendanceStatus,
                    opt => opt.MapFrom(src => src.AttendanceStatus));

            CreateMap<CreateMeetingDto, Meeting>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.Attendees, opt => opt.Ignore());

            CreateMap<UpdateMeetingDto, Meeting>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.Attendees, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            // Invoice mappings
            CreateMap<Invoice, InvoiceDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}" : null))
                .ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderNumber : null))
                .ForMember(dest => dest.CreatedByPersonelName, opt => opt.MapFrom(src => src.CreatedByPersonel != null ? $"{src.CreatedByPersonel.FirstName} {src.CreatedByPersonel.LastName}" : null))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payments));

            CreateMap<InvoiceItem, InvoiceItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product != null ? src.Product.Sku : null));

            CreateMap<Payment, PaymentDto>()
                .ForMember(dest => dest.ReceivedByPersonelName, opt => opt.MapFrom(src => src.ReceivedByPersonel != null ? $"{src.ReceivedByPersonel.FirstName} {src.ReceivedByPersonel.LastName}" : null));

            CreateMap<CreateInvoiceDto, Invoice>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InvoiceNumber, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
                .ForMember(dest => dest.TaxAmount, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.PaidAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore());

            CreateMap<CreateInvoiceItemDto, InvoiceItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InvoiceId, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore());

            CreateMap<UpdateInvoiceDto, Invoice>()
                .ForMember(dest => dest.InvoiceNumber, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
                .ForMember(dest => dest.TaxAmount, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.PaidAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Quote mappings
            CreateMap<Quote, QuoteDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}" : null))
                .ForMember(dest => dest.OpportunityName, opt => opt.MapFrom(src => src.Opportunity != null ? src.Opportunity.Name : null))
                .ForMember(dest => dest.CreatedByPersonelName, opt => opt.MapFrom(src => src.CreatedByPersonel != null ? $"{src.CreatedByPersonel.FirstName} {src.CreatedByPersonel.LastName}" : null))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<QuoteItem, QuoteItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product != null ? src.Product.Sku : null));

            CreateMap<CreateQuoteDto, Quote>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.QuoteNumber, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
                .ForMember(dest => dest.TaxAmount, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore());

            CreateMap<CreateQuoteItemDto, QuoteItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.QuoteId, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore());

            CreateMap<UpdateQuoteDto, Quote>()
                .ForMember(dest => dest.QuoteNumber, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
                .ForMember(dest => dest.TaxAmount, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            // ========== CONTRACT MAPPINGS ==========
            CreateMap<Contract, ContractDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}" : null))
                .ForMember(dest => dest.CreatedByPersonelName, opt => opt.MapFrom(src => src.CreatedByPersonel != null ? $"{src.CreatedByPersonel.FirstName} {src.CreatedByPersonel.LastName}" : null))
                .ForMember(dest => dest.QuoteNumber, opt => opt.MapFrom(src => src.Quote != null ? src.Quote.QuoteNumber : null));

            CreateMap<CreateContractDto, Contract>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ContractNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.IsSigned, opt => opt.Ignore())
                .ForMember(dest => dest.SignedDate, opt => opt.Ignore())
                .ForMember(dest => dest.SignedBy, opt => opt.Ignore());

            CreateMap<UpdateContractDto, Contract>()
                .ForMember(dest => dest.ContractNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.IsSigned, opt => opt.Ignore())
                .ForMember(dest => dest.SignedDate, opt => opt.Ignore())
                .ForMember(dest => dest.SignedBy, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            // ========== CAMPAIGN MAPPINGS ==========
            CreateMap<Campaign, CampaignDto>()
                .ForMember(dest => dest.CreatedByPersonelName, opt => opt.MapFrom(src => src.CreatedByPersonel != null ? $"{src.CreatedByPersonel.FirstName} {src.CreatedByPersonel.LastName}" : null));

            CreateMap<CreateCampaignDto, Campaign>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore());

            CreateMap<UpdateCampaignDto, Campaign>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ========== OPPORTUNITY MAPPINGS ==========
            CreateMap<Opportunity, OpportunityDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}" : null))
                .ForMember(dest => dest.AssignedToPersonelName, opt => opt.MapFrom(src => src.AssignedToPersonel != null ? $"{src.AssignedToPersonel.FirstName} {src.AssignedToPersonel.LastName}" : null))
                .ForMember(dest => dest.CreatedByPersonelName, opt => opt.MapFrom(src => src.CreatedByPersonel != null ? $"{src.CreatedByPersonel.FirstName} {src.CreatedByPersonel.LastName}" : null));

            CreateMap<CreateOpportunityDto, Opportunity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.ActualCloseDate, opt => opt.Ignore())
                .ForMember(dest => dest.LostReason, opt => opt.Ignore());

            CreateMap<UpdateOpportunityDto, Opportunity>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            // ========== TASK MAPPINGS ==========
            CreateMap<DomainTask, TaskDto>()
                .ForMember(dest => dest.AssignedToPersonelName, opt => opt.MapFrom(src => src.AssignedToPersonel != null ? $"{src.AssignedToPersonel.FirstName} {src.AssignedToPersonel.LastName}" : null))
                .ForMember(dest => dest.RelatedToCustomerName, opt => opt.MapFrom(src => src.RelatedToCustomer != null ? $"{src.RelatedToCustomer.FirstName} {src.RelatedToCustomer.LastName}" : null))
                .ForMember(dest => dest.RelatedToLeadName, opt => opt.MapFrom(src => src.RelatedToLead != null ? src.RelatedToLead.CompanyName : null))
                .ForMember(dest => dest.RelatedToOpportunityName, opt => opt.MapFrom(src => src.RelatedToOpportunity != null ? src.RelatedToOpportunity.Name : null))
                .ForMember(dest => dest.CreatedByPersonelName, opt => opt.MapFrom(src => src.CreatedByPersonel != null ? $"{src.CreatedByPersonel.FirstName} {src.CreatedByPersonel.LastName}" : null));

            CreateMap<CreateTaskDto, DomainTask>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedAt, opt => opt.Ignore());

            CreateMap<UpdateTaskDto, DomainTask>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByPersonelId, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedAt, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}