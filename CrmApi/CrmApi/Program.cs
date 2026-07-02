using AspNetCoreRateLimit;
using Crm.Application.Mappings;
using Crm.Infrastructure.Data;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Hubs;
using CrmApi.Services;
using CrmApi.Validators.ProductValidator;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using System.Globalization;
using System.Text;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF Community lisansını ayarla
QuestPDF.Settings.License = LicenseType.Community;

// ========== 0. TÜRKÇE DİL SABİTLEME ==========
var cultureInfo = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// ========== 1. TEMEL SERVISLER ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
 
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ========== 2. DATABASE ==========
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ========== 3. AUTOMAPPER ==========
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// ========== 4. UNIT OF WORK ==========
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ========== 5. SIGNALR ==========
builder.Services.AddSignalR();

// ========== 6. FLUENTVALIDATION ==========
// Validator'ları DI'a ekle
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDtoValidator>();

// Controller'ları ekle
builder.Services.AddControllers();

//  SharpGrip ayarı - basit versiyon
builder.Services.AddFluentValidationAutoValidation();

// ========== 7. RATE LIMITING ==========
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
         // Genel limit: Dakikada 500 istek (development için yeterli)
        new RateLimitRule { Endpoint = "*", Limit = 500, Period = "1m" },
        // Login: Dakikada 10 deneme
        new RateLimitRule { Endpoint = "POST:/api/Auth/login", Limit = 10, Period = "1m" },
        // Register: Dakikada 5 deneme
        new RateLimitRule { Endpoint = "POST:/api/Auth/register", Limit = 5, Period = "1m" },
        new RateLimitRule { Endpoint = "POST:/api/Auth/forgot-password", Limit = 3, Period = "15m" },

         // Dashboard: Dakikada 60 istek (her saniye 1 istek)
        new RateLimitRule { Endpoint = "GET:/api/Dashboard/*", Limit = 60, Period = "1m" },
        
        // Report: Dakikada 30 istek
        new RateLimitRule { Endpoint = "POST:/api/Report/*", Limit = 30, Period = "1m" },
    };
    options.StackBlockedRequests = true;
});
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ========== 8. CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("https://localhost:5173", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ========== 9. JWT AUTHENTICATION ==========
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
            ValidAudience = builder.Configuration["JWT:ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// ========== 10. SERVISLER ==========
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISystemAdminService, SystemAdminService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IDataFilterService, DataFilterService>();
builder.Services.AddHostedService<InvoiceStatusService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddHttpContextAccessor();

// ==========  EXCHANGE RATE SERVICE  ==========
builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>();
//builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();

// ========== 11. API BEHAVIOR ==========
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false;

    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        return new BadRequestObjectResult(new
        {
            message = "Doğrulama hatası",
            errors = errors,
            statusCode = 400
        });
    };
});

var app = builder.Build();

// ========== MIDDLEWARE PIPELINE ==========
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:;");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }

    context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
    context.Response.Headers.Add("Pragma", "no-cache");

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowReactApp");
app.UseIpRateLimiting();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(dbContext);
}

app.Run();