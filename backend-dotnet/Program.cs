using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using MyKopdes.Configuration;
using MyKopdes.Middleware;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.HttpOverrides;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseJsonNamingPolicy();
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MyKopdes API",
        Version = "v1",
        Description = "Koperasi store management API — auth, items, members, transactions, loans, reports. " +
                      "Staff endpoints accept a JWT from POST /api/auth/staff/login; member endpoints one from POST /api/auth/member/login."
    });

    // Use the full type name for schema ids so two DTOs with the same simple
    // name in different namespaces can never collide (that 500s the doc).
    options.CustomSchemaIds(t => t.FullName!.Replace("+", "."));

    // JWT bearer: adds the Authorize button on locked endpoints so they can
    // be tried straight from Swagger UI.
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste the token from /api/auth/staff/login or /api/auth/member/login (no 'Bearer ' prefix needed)."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });

    // Pick up /// comments from controllers/models into the UI.
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});


// Compress JSON responses (gzip) — smaller payloads means faster tables and
// dashboards on the client. Nginx proxies them through transparently.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = false;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
});
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.AddMemoryCache();

// Database - MySQL with existing schema (no migrations, no schema changes).
// Pooled contexts are cheaper per request than a fresh DbContext, and a fixed
// server version avoids a live DB round trip just to detect the version at
// startup (which also made bootup depend on MySQL being reachable first).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "server=mysql;port=3306;database=koperasi;user=koperasi;password=koperasi_secret;SslMode=none;AllowPublicKeyRetrieval=true";

builder.Services.AddDbContextPool<KoperasiDbContext>(options =>
    options.UseMySql(connectionString, KoperasiDbContext.FixedServerVersion));

// JWT Authentication — fallback to environment variables for Docker
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? "KoperasiSuperSecretKeyForJwtTokens2024!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? "http://localhost";
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "http://localhost";
var jwtExpiry = builder.Configuration.GetValue<int>("Jwt:ExpiryMinutes", 1440);

var jwtSettings = new JwtSettings
{
    Key = jwtKey,
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    ExpiryMinutes = jwtExpiry
};
builder.Services.Configure<JwtSettings>(options =>
{
    options.Key = jwtKey;
    options.Issuer = jwtIssuer;
    options.Audience = jwtAudience;
    options.ExpiryMinutes = jwtExpiry;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IRestockService, RestockService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IShuService, ShuService>();
builder.Services.AddScoped<IItemCategoryService, ItemCategoryService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<ILoanProductService, LoanProductService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
// The TLS termination happens at the nginx container, so no HTTPS redirect
// here (UseHttpsRedirection is a no-op behind the proxy anyway).
// Swagger UI is served in Production as well (the deployment is an internal
// docker network) so the API can be explored/tested live; to restrict it,
// gate these two lines on app.Environment.IsDevelopment().
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyKopdes API v1");
    options.DocumentTitle = "MyKopdes API Docs";
});

// Behind nginx: trust X-Forwarded-For/Proto so logs and audit entries carry
// the real client IP instead of nginx's container IP.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Trust the docker bridge networks so nginx's forwarded headers are used.
    KnownNetworks = { Microsoft.AspNetCore.HttpOverrides.IPNetwork.Parse("172.16.0.0/12") },
    KnownProxies = { System.Net.IPAddress.Parse("127.0.0.1") }
});

app.UseResponseCompression();
app.UseCorsPolicy();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestLoggingMiddleware>();
app.MapControllers();

// Health check endpoint (for nginx healthcheck)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

public partial class Program { }
