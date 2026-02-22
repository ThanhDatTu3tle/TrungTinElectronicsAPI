using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TrungTinElectronicsAPI.Data;
using TrungTinElectronicsAPI.Models;
using TrungTinElectronicsAPI.Repositories.Category;
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
        ValidateIssuer = false, // vài bữa deploy thì set lại là true
        ValidateAudience = false, // vài bữa deploy thì set lại là true
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
                    "http://localhost:5225"         // Swagger HTTP (nếu có)
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
builder.Services.AddSwaggerGen();

// Inject repository Product
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Inject repository Category
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Inject Cloudinary
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<CloudinaryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Dùng CORS trước khi map controller
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
