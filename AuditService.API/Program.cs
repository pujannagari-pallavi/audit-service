using AuditService.API.Contracts.NotificationService;
using AuditService.API.Contracts.ObservationService;
using AuditService.API.Contracts.ReportingService;
using AuditService.API.Converters;
using AuditService.API.Data;
using AuditService.API.HttpClients;
using AuditService.API.Repositories;
using AuditService.API.Repositories.Interfaces;
using AuditService.API.Services;
using AuditService.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuditDb")));

builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService.API.Services.AuditService>();

// Configure AutoMapper
builder.Services.AddSingleton<IMapper>(provider =>
{
    var config = new MapperConfiguration(cfg =>
    {
        cfg.AddMaps(typeof(Program).Assembly);
    }, provider.GetService<ILoggerFactory>());
    return config.CreateMapper();
});

// Register HttpClient for Observation Service communication
builder.Services.AddHttpClient<IObservationServiceClient, ObservationServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ObservationService"]!);
});

// Register HttpClient for Reporting Service communication
builder.Services.AddHttpClient<IReportingServiceClient, ReportingServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ReportingService"]!);
});

// Register HttpClient for Notification Service communication
builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:NotificationService"]!);
});

// CORS for frontend apps (local dev + deployed frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "https://frontend-enhanced-audit-management.onrender.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Secret"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AuditorOrAbove", policy => policy.RequireRole("Admin", "Auditor", "AuditManager"));
    options.AddPolicy("ManagerOrAbove", policy => policy.RequireRole("Admin", "AuditManager"));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateOnlyJsonConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Audit Service API", Version = "v1" });
});

var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AuditDbContext>();
        context.Database.Migrate();
        Console.WriteLine("AuditService: Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "Unhandled exception occurred");
        
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = exception?.Message ?? "An unexpected error occurred",
            errors = new[] { exception?.ToString() ?? "Unknown error" }
        });
    });
});

// Enable Swagger for all environments (useful for API testing)
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Audit Service API v1"));

// HTTPS redirection disabled for Render deployment (Render handles HTTPS at load balancer)
// app.UseHttpsRedirection();

app.UseCors("FrontendCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Root endpoint for service discovery on platforms that probe '/'
app.MapGet("/", () => Results.Ok(new
{
    service = "AuditService",
    status = "running",
    docs = "/swagger",
    health = "/health"
}));

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AuditService" }));

app.Run();
