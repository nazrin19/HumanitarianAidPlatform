using HumanitarianAidPlatform.Api.Middleware;
using HumanitarianAidPlatform.Api.Swagger;
using HumanitarianAidPlatform.Application.Ai;
using HumanitarianAidPlatform.Application.Admin;
using HumanitarianAidPlatform.Application.Auth;
using HumanitarianAidPlatform.Application.Donors;
using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Application.Requests;
using HumanitarianAidPlatform.Application.Resources;
using HumanitarianAidPlatform.Application.Shelters;
using HumanitarianAidPlatform.Application.Volunteers;
using HumanitarianAidPlatform.Infrastructure.Ai;
using HumanitarianAidPlatform.Infrastructure.External;
using HumanitarianAidPlatform.Infrastructure.Notifications;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using HumanitarianAidPlatform.Infrastructure.Repositories.Donors;
using HumanitarianAidPlatform.Infrastructure.Repositories.Requests;
using HumanitarianAidPlatform.Infrastructure.Repositories.Resources;
using HumanitarianAidPlatform.Infrastructure.Repositories.Shelters;
using HumanitarianAidPlatform.Infrastructure.Repositories.Users;
using HumanitarianAidPlatform.Infrastructure.Repositories.Volunteers;
using HumanitarianAidPlatform.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HumanitarianAidPlatform.Application.Campaigns;
using HumanitarianAidPlatform.Infrastructure.Repositories.Campaigns;
using HumanitarianAidPlatform.Application.Donations;
using HumanitarianAidPlatform.Application.Geocoding;
using HumanitarianAidPlatform.Infrastructure.Repositories.Donations;
using System;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);

const string ReactDevelopmentCors = "ReactDevelopment";

builder.Services.AddCors(options =>
{
    options.AddPolicy(ReactDevelopmentCors, policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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

// AI classifier (Strategy pattern): the "Ai:Classifier" setting picks which one is used.
var classifierName = builder.Configuration["Ai:Classifier"] ?? "RuleBased";
if (classifierName == "Default")
    builder.Services.AddSingleton<IUrgencyClassifier, DefaultUrgencyClassifier>();
else
    builder.Services.AddSingleton<IUrgencyClassifier, RuleBasedUrgencyClassifier>();

// Authentication services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

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

// Donors & Volunteers
builder.Services.AddScoped<IDonorRepository, DonorRepository>();
builder.Services.AddScoped<IDonorService, DonorService>();
builder.Services.AddScoped<IVolunteerRepository, VolunteerRepository>();
builder.Services.AddScoped<IVolunteerService, VolunteerService>();
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IDonationRepository, DonationRepository>();
builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddScoped<IVolunteerTaskRepository, VolunteerTaskRepository>();
builder.Services.AddScoped<IVolunteerTaskService, VolunteerTaskService>();

// Nominatim provider configuration and typed HTTP client
builder.Services.AddOptions<NominatimOptions>()
    .Bind(builder.Configuration.GetSection(NominatimOptions.SectionName))
    .Validate(
        options =>
            Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) &&
            baseUri.Scheme == Uri.UriSchemeHttps &&
            !string.IsNullOrWhiteSpace(options.UserAgent) &&
            options.UserAgent.Contains('@') &&
            options.TimeoutSeconds is >= 1 and <= 120 &&
            options.MaximumCandidates is >= 2 and <= 10 &&
            options.MaximumRetries is >= 0 and <= 2,
        "Nominatim configuration requires an HTTPS base URL, a contact-bearing User-Agent, a 1-120 second timeout, 2-10 candidates, and 0-2 retries.")
    .ValidateOnStart();

// HttpClientFactory logs request URIs, which contain user-entered location text.
builder.Logging.AddFilter(
    "System.Net.Http.HttpClient.IGeocodingService",
    LogLevel.None);

builder.Services.AddHttpClient<IGeocodingService, MappingClient>(
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<NominatimOptions>>()
            .Value;
        client.BaseAddress = new Uri(
            options.BaseUrl.EndsWith("/", StringComparison.Ordinal)
                ? options.BaseUrl
                : $"{options.BaseUrl}/",
            UriKind.Absolute);
        client.Timeout = Timeout.InfiniteTimeSpan;
        client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
    });

// Notification services
builder.Services.AddSingleton<NotificationDeliveryDeduplicator>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<ISmsSender, SmsSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

if (args.Contains("--reset-admin-password", StringComparer.Ordinal))
{
    if (args.Length != 1)
    {
        throw new InvalidOperationException(
            "--reset-admin-password must be the only application argument.");
    }

    await ResetAdminPasswordAsync(app);
    return;
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors(ReactDevelopmentCors);

if (app.Configuration.GetValue<bool>("AdminBootstrap:Enabled"))
{
    using var scope = app.Services.CreateScope();
    var adminUsers = scope.ServiceProvider.GetRequiredService<IAdminUserService>();
    var created = await adminUsers.BootstrapFirstAdminAsync(
        app.Configuration["AdminBootstrap:Email"],
        app.Configuration["AdminBootstrap:Password"],
        CancellationToken.None);
    if (created)
        app.Logger.LogWarning("A first administrator account was bootstrapped. Remove the bootstrap secret after use.");
}

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

static async Task ResetAdminPasswordAsync(WebApplication app)
{
    Console.Write("Existing Admin email: ");
    var email = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(email))
    {
        Console.Error.WriteLine("An Admin email is required.");
        Environment.ExitCode = 1;
        return;
    }

    var password = ReadHiddenInput("New password (12+ characters): ");
    var confirmation = ReadHiddenInput("Confirm new password: ");
    if (!string.Equals(password, confirmation, StringComparison.Ordinal))
    {
        Console.Error.WriteLine("The passwords did not match. No changes were made.");
        Environment.ExitCode = 1;
        return;
    }

    await using var scope = app.Services.CreateAsyncScope();
    var adminUsers = scope.ServiceProvider.GetRequiredService<IAdminUserService>();
    var reset = await adminUsers.ResetActiveAdminPasswordAsync(
        email,
        password,
        CancellationToken.None);
    if (!reset)
    {
        Console.Error.WriteLine("No active Admin account was found for that email. No changes were made.");
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine("Admin password updated. Start the API normally and sign in with the new password.");
}

static string ReadHiddenInput(string prompt)
{
    Console.Write(prompt);
    var value = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return value.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (value.Length > 0)
            {
                value.Length--;
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            value.Append(key.KeyChar);
        }
    }
}