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

// Database
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
        "Missing 'Jwt' section in appsettings.json");

// Authentication
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

// Shelter services
builder.Services.AddScoped<IShelterRepository, ShelterRepository>();
builder.Services.AddScoped<ShelterService>();

// Resource services
builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
builder.Services.AddScoped<ResourceService>();

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();