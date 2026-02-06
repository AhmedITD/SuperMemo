using System.Security.Claims;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SuperMemo.Api.Json;
using SuperMemo.Application;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Domain.Enums;
using SuperMemo.Infrastructure.Constants;
using SuperMemo.Infrastructure.Data;
using SuperMemo.Infrastructure.Data.Interceptors;
using SuperMemo.Application.Common;
using SuperMemo.Application.Interfaces.Payroll;
using SuperMemo.Application.Interfaces.Storage;
using SuperMemo.Infrastructure.Options;
using SuperMemo.Infrastructure.Services;
using SuperMemo.Infrastructure.Services.Auth;
using SuperMemo.Infrastructure.Services.Sinks;
using SuperMemo.Infrastructure.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(ms => ms.Value?.Errors?.Count > 0)
                .ToDictionary(
                    ms => ms.Key,
                    ms => ms.Value!.Errors.Select(e => e.ErrorMessage ?? "").ToArray());
            var response = ApiResponse<object>.ErrorResponse("Validation failed.", errors, code: ErrorCodes.ValidationFailed);
            return new BadRequestObjectResult(response);
        };
    });
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "SuperMemo API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token."
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
});

builder.Services.AddScoped<AuditLogInterceptor>();
builder.Services.AddScoped<IAuditLogSink, EfCoreAuditLogSink>();
builder.Services.AddScoped<IAuditEventLogger, AuditEventLogger>();

builder.Services.AddDbContext<SuperMemoDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .AddInterceptors(sp.GetRequiredService<AuditLogInterceptor>());
});
builder.Services.AddScoped<ISuperMemoDbContext, SuperMemoDbContext>();

builder.Services.AddApplicationServices();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.Configure<PayrollOptions>(builder.Configuration.GetSection(PayrollOptions.SectionName));
builder.Services.AddScoped<IPayrollRunnerService, PayrollRunnerService>();
builder.Services.AddHostedService<PayrollRunnerHostedService>();

// Transaction processing background services
builder.Services.AddHostedService<SuperMemo.Infrastructure.Services.TransactionProcessingHostedService>();
builder.Services.AddHostedService<SuperMemo.Infrastructure.Services.TransactionExpirationHostedService>();
builder.Services.AddHostedService<SuperMemo.Infrastructure.Services.TransactionAutoRetryHostedService>();

// Phase 8 background services
builder.Services.AddHostedService<SuperMemo.Infrastructure.Services.InterestCalculationHostedService>();
builder.Services.AddHostedService<SuperMemo.Infrastructure.Services.DailyLimitResetHostedService>();

// Storage: resolve paths to content root so uploads are under the app directory
builder.Services.Configure<StorageOptions>(options =>
{
    var section = builder.Configuration.GetSection(StorageOptions.SectionName);
    section.Bind(options);
    if (!Path.IsPathRooted(options.BasePath))
        options.BasePath = Path.Combine(builder.Environment.ContentRootPath, options.BasePath);
    if (!Path.IsPathRooted(options.UserImagesPath))
        options.UserImagesPath = Path.Combine(builder.Environment.ContentRootPath, options.UserImagesPath);
});
builder.Services.AddScoped<IStorageService, FileStorageService>();

builder.Services.AddHttpClient<IOtpiqService, OtpiqService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(UserRole.Admin.ToStringValue(), policy =>
        policy.RequireClaim(ClaimTypes.Role, UserRole.Admin.ToStringValue()));
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = AuthConstants.JwtScheme;
    options.DefaultChallengeScheme = AuthConstants.JwtScheme;
    options.DefaultScheme = AuthConstants.JwtScheme;
})
.AddJwtBearer(AuthConstants.JwtScheme, options =>
{
    var secret = builder.Configuration["JwtSettings:Secret"];
    if (string.IsNullOrEmpty(secret))
        throw new InvalidOperationException("JwtSettings:Secret is not set in configuration.");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
// Serve uploaded KYC images from /uploads/kyc
var storageSection = app.Configuration.GetSection(StorageOptions.SectionName);
var baseUrl = storageSection["BaseUrl"]?.TrimEnd('/') ?? "/uploads/kyc";
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (Directory.Exists(uploadsPath))
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });
app.UseSwagger();
app.UseSwaggerUI(o => o.DisplayRequestDuration());

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(_ => { });

app.MapControllers();

app.Run();
