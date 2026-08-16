using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SEAL_Application.Commons;
using SEAL_Application.Interfaces;
using SEAL_Application.Services;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Infrastructure.Persistence;
using SEAL_Infrastructure.Persistence.Seeding;
using SEAL_Infrastructure.Services;
using SEAL_Infrastructure.UnitOfWork;
using System;
using System.IO;
using System.Text;

namespace SEAL_Backend.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            // MediatR & Pipeline Behaviors
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IUnitOfWork).Assembly));
            services.AddValidatorsFromAssembly(typeof(IUnitOfWork).Assembly);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Database Context & UnitOfWork
            var connectionString = DatabaseConnectionHelper.BuildConnectionString(configuration);
            services.AddDbContext<DatabaseContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEventMetadataResolver, EventMetadataResolver>();
            services.AddMemoryCache();
            services.AddScoped<IEventRoleChecker, EventRoleChecker>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IEmailService, EmailService>();

            // External Services & HttpClients
            services.AddHttpClient<IGitHostingService, GitHostingService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SEAL-Hackathon/1.0");
            });
            services.AddSingleton<ICloudStorageService, CloudflyStorageService>();
            services.AddHttpClient();

            // Data Seeders
            services.AddDataSeeders();

            // Options Configurations
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<AdminSettings>(configuration.GetSection("AdminSettings"));

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

            if (jwtSettings is null || Encoding.UTF8.GetByteCount(jwtSettings.SecretKey ?? string.Empty) < 64)
            {
                throw new InvalidOperationException(
                    "JwtSettings:SecretKey thiếu hoặc ngắn hơn 64 byte. HMAC-SHA512 cần khóa >= 64 byte. " +
                    "Đặt biến môi trường JwtSettings__SecretKey (>= 64 ký tự) trên môi trường deploy rồi khởi động lại.");
            }

            services.AddAuthentication(options =>
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey!)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            return services;
        }

        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập token JWT theo định dạng: Bearer {token}"
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

            return services;
        }

        public static IServiceCollection AddCustomCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
