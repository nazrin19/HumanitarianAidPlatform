
using System;
using System.Text;
using System.Text.Json.Serialization;

using HumanitarianAidPlatform.Api.Swagger;
using HumanitarianAidPlatform.Application.Auth;
using HumanitarianAidPlatform.Application.Requests;
using HumanitarianAidPlatform.Application.Shelters;
using HumanitarianAidPlatform.Application.Resources;

using HumanitarianAidPlatform.Infrastructure.Persistence;
using HumanitarianAidPlatform.Infrastructure.Repositories.Requests;
using HumanitarianAidPlatform.Infrastructure.Repositories.Users;
using HumanitarianAidPlatform.Infrastructure.Repositories.Shelters;
using HumanitarianAidPlatform.Infrastructure.Repositories.Resources;
using HumanitarianAidPlatform.Infrastructure.Security;
<<<<<<< HEAD
using HumanitarianAidPlatform.Application.Donors;
using HumanitarianAidPlatform.Application.Volunteers;
using HumanitarianAidPlatform.Infrastructure.Repositories.Donors;
using HumanitarianAidPlatform.Infrastructure.Repositories.Volunteers;
=======

>>>>>>> 324a544ad02db004f07c7c5932e04c0a17534eda
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Controllers and JSON configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });

// OpenAPI and JWT security documentation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// Database configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")));

// JWT settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

var jwt = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "Missing 'Jwt' section in configuration");

if (string.IsNullOrWhiteSpace(jwt.Key))
{
    throw new InvalidOperationException(
        "JWT signing key is missing. Check User Secrets.");
}

// Authentication configuration
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,

                ValidateAudience = true,
                ValidAudience = jwt.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.Key)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),

                NameClaimType = "name",
                RoleClaimType = "role"
            };
    });

builder.Services.AddAuthorization();

// Request services
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<IRequestService, RequestService>();

// Authentication services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

<<<<<<< HEAD
// Donors & Volunteers
builder.Services.AddScoped<IDonorRepository, DonorRepository>();
builder.Services.AddScoped<IDonorService, DonorService>();
builder.Services.AddScoped<IVolunteerRepository, VolunteerRepository>();
builder.Services.AddScoped<IVolunteerService, VolunteerService>();
=======
// Shelter services
builder.Services.AddScoped<IShelterRepository, ShelterRepository>();
builder.Services.AddScoped<ShelterService>();

// Resource services
builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
builder.Services.AddScoped<ResourceService>();

// Resource Allocation services
builder.Services.AddScoped<
    IResourceAllocationRepository,
    ResourceAllocationRepository>();

builder.Services.AddScoped<ResourceAllocationService>();
>>>>>>> 324a544ad02db004f07c7c5932e04c0a17534eda

var app = builder.Build();

// API documentation
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Humanitarian Aid API v1");
    });
}

// HTTPS redirection outside local Development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
