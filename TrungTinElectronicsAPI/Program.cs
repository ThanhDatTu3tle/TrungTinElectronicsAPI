using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;
using TrungTinElectronics.Jobs;
using TrungTinElectronics.Repositories;
using TrungTinElectronics.Services;
using TrungTinElectronicsAPI.Data;
using TrungTinElectronicsAPI.Jobs;
using TrungTinElectronicsAPI.Models;
using TrungTinElectronicsAPI.Repositories;
using TrungTinElectronicsAPI.Repositories.BulkImport;
using TrungTinElectronicsAPI.Repositories.Category;
using TrungTinElectronicsAPI.Repositories.Event;
using TrungTinElectronicsAPI.Repositories.Product;
using TrungTinElectronicsAPI.Services;

var builder = WebApplication.CreateBuilder(args);
// optional: load user-secrets already done earlier

// đọc key từ config / user-secrets
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT key not configured. Set Jwt:Key in user-secrets or appsettings.");
}
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

// cấu hình authentication + jwt
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, // vài bữa deploy thì set lại là true
        ValidateAudience = true, // vài bữa deploy thì set lại là true
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// --- Swagger Config ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TrungTinElectronics API", Version = "v1106.15" });

    // Thêm security định nghĩa
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT token theo dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Áp dụng global requirement
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// authorization
builder.Services.AddAuthorization();

// Đọc connection string theo môi trường (Development hay Production)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:4200",        // Angular
                    "https://localhost:5225",       // Swagger HTTPS
                    "http://localhost:5225",         // Swagger HTTP (nếu có)
                    "http://45.119.82.33",
                    "http://trungtinelectronics.com",
                    "https://trungtinelectronics.com"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<OrderService>();
//builder.Services.AddSwaggerGen();

// Inject repository Product
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Inject repository Category
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Inject repository Event
builder.Services.AddScoped<IEventRepository, EventRepository>();

// Inject Cloudinary
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<CloudinaryService>();

// Bulk import products
builder.Services.AddScoped<IProductBulkImportRepository, ProductBulkImportRepository>();
builder.Services.AddScoped<IProductBulkImportService, ProductBulkImportService>();

// Jobs
builder.Services.AddScoped<CancelExpiredOrdersJob>();

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
        }
    )
);

builder.Services.AddHangfireServer();

var redisConn = await ConnectionMultiplexer.ConnectAsync(
    builder.Configuration.GetConnectionString("Redis")!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConn);
builder.Services.AddSingleton<RedisQueueService>();

// Worker chạy nền xử lý payment queue
builder.Services.AddHostedService<PaymentCallbackWorker>();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

// 1. Middleware trước
app.UseSwagger();
app.UseSwaggerUI();

// Dùng CORS trước khi map controller
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 2. Hangfire dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAllowAllFilter() }
});

// Đăng ký recurring jobs
RecurringJob.AddOrUpdate<NotificationCleanupJob>(
    "notification-cleanup",
    job => job.Execute(),
    Cron.Daily(3, 0)
);

// 3. Đăng ký Cron job SAU khi app đã build xong
app.Lifetime.ApplicationStarted.Register(() =>
{
    RecurringJob.AddOrUpdate<CancelExpiredOrdersJob>(
        recurringJobId: "cancel-expired-orders",
        methodCall: job => job.ExecuteAsync(),
        cronExpression: "*/5 * * * *"
    );
});

app.Run();

public class HangfireAllowAllFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
