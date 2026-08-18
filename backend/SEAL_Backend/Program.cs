using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Backend.Middlewares;
using SEAL_Domain.Base;
using SEAL_Infrastructure.Persistence;
using SEAL_Infrastructure.Persistence.Seeding;
using SEAL_Infrastructure.Services;
using SEAL_Infrastructure.UnitOfWork;

// Neu chua ai set ASPNETCORE_ENVIRONMENT (vd chay "dotnet run --no-launch-profile"), .NET mac dinh
// roi vao Production -> vo tinh noi nham DB that tren Render thay vi DB local. Render luon tu set bien
// nay khi deploy nen fallback duoi day KHONG anh huong production, chi bao ve may local.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Scans the SEAL_Application assembly for MediatR handlers. IUnitOfWork is used purely as an
// assembly marker (lives in that project) — each flow's Features/ handlers get picked up
// automatically once added, no change needed here.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IUnitOfWork).Assembly));

// Register FluentValidation and Pipeline Behavior
builder.Services.AddValidatorsFromAssembly(typeof(IUnitOfWork).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SEAL_Application.Commons.ValidationBehavior<,>));

// Register DbContext and UnitOfWork
var connectionString = DatabaseConnectionHelper.BuildConnectionString(builder.Configuration);
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEventMetadataResolver, SEAL_Application.Services.EventMetadataResolver>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IEventRoleChecker, SEAL_Application.Services.EventRoleChecker>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuditLogService, SEAL_Application.Services.AuditLogService>();
builder.Services.AddScoped<INotificationService, SEAL_Application.Services.NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<IGitHostingService, GitHostingService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SEAL-Hackathon/1.0");
});
// Dev local + chua co key S3 that (van con placeholder "YOUR_...") -> dung storage
// luu xuong dia cuc bo thay vi CloudFly that, de test upload/xem anh duoc tren may
// ma khong can key that (thuong chi ton tai tren Render production). Set key S3 that
// vao appsettings.Development.json (gitignored) se tu dong chuyen ve dung CloudFly that.
var s3AccessKey = builder.Configuration.GetSection("S3").GetValue<string>("AccessKeyId");
var useLocalStorage = builder.Environment.IsDevelopment()
    && (string.IsNullOrWhiteSpace(s3AccessKey) || s3AccessKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase));

if (useLocalStorage)
{
    builder.Services.AddSingleton<ICloudStorageService, LocalFileStorageService>();
}
else
{
    builder.Services.AddSingleton<ICloudStorageService, CloudflyStorageService>();
}
builder.Services.AddHttpClient();

// Seeding
builder.Services.AddDataSeeders();

// Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// Token ky bang HMAC-SHA512 -> khoa BAT BUOC >= 64 byte. JwtSettings.IsValid()
// chi kiem tra rong nen khoa ngan (vd env var deploy dat sai) van lot qua boot
// roi nem loi dung luc ky token -> moi login thanh cong deu 500 "unexpected error".
// Chan tu dau: chet ngay khi khoi dong voi thong bao ro, hien trong log deploy.
if (jwtSettings is null || Encoding.UTF8.GetByteCount(jwtSettings.SecretKey ?? string.Empty) < 64)
{
    throw new InvalidOperationException(
        "JwtSettings:SecretKey thieu hoac ngan hon 64 byte. HMAC-SHA512 can khoa >= 64 byte. " +
        "Dat bien moi truong JwtSettings__SecretKey (>= 64 ky tu) tren moi truong deploy roi khoi dong lai.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings!.SecretKey!)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\""
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
// FE local (.env.local: NEXT_PUBLIC_API_URL=http://localhost:5138/api) luon goi HTTP,
// khong bao gio HTTPS. Redirect sang https://localhost:7153 chi hai: neu chi bind HTTP
// (vd chay bang "dotnet run --urls http://...") request treo/loi ket noi; con khi ca 2
// cong deu bind thi trinh duyet co the chua tin cay chung chi dev tu ky, van fail. Production
// dung domain Render rieng qua HTTPS that, khong phu thuoc middleware nay.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
// Phuc vu file tinh trong wwwroot/uploads khi dung LocalFileStorageService. PHAI tao
// thu muc TRUOC khi goi UseStaticFiles: neu wwwroot chua ton tai luc app khoi dong,
// env.WebRootFileProvider mac dinh bi "khoa" thanh NullFileProvider vinh vien cho ca
// vong doi process (dinh nghia trong PhysicalFileProvider luc build host) — dung
// StaticFileOptions voi FileProvider tuong minh de tranh phu thuoc thoi diem khoi dong.
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
});
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.Run();
