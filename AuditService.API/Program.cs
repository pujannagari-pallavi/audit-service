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
using Serilog;
using Serilog.Context;
using Serilog.Sinks.Elasticsearch;
using System.Text;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
const string CorrelationIdHeader = "X-Correlation-ID";

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "AuditService")
        .WriteTo.Console()
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(context.Configuration["Elasticsearch:Uri"]!))
        {
            IndexFormat = "audit-service-logs-{0:yyyy.MM.dd}",
            AutoRegisterTemplate = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
            ModifyConnectionSettings = conn => conn.BasicAuthentication(
                context.Configuration["Elasticsearch:Username"]!,
                context.Configuration["Elasticsearch:Password"]!
            ),
            MinimumLogEventLevel = Serilog.Events.LogEventLevel.Information
        });
});

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

// Register HttpClient for Observation Service communication
builder.Services.AddHttpClient<IObservationServiceClient, ObservationServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ObservationService"]!);
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

// Register HttpClient for Reporting Service communication
builder.Services.AddHttpClient<IReportingServiceClient, ReportingServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ReportingService"]!);
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

// Register HttpClient for Notification Service communication
builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:NotificationService"]!);
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

// CORS for frontend apps (local dev + deployed frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;

                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;

                // Allow local frontend dev servers on any port (e.g., 5173, 5174)
                if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;

                // Allow deployed frontend
                return origin.Equals(
                    "https://frontend-enhanced-audit-management.onrender.com",
                    StringComparison.OrdinalIgnoreCase
                );
            })
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
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(context.Exception, "JWT authentication failed for path {Path}", context.HttpContext.Request.Path);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.Principal?.FindFirst("sub")?.Value
                ?? "unknown";
            logger.LogInformation("JWT token validated for UserId {UserId} on path {Path}", userId, context.HttpContext.Request.Path);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT challenge triggered for path {Path}. Error: {Error}", context.Request.Path, context.Error);
            return Task.CompletedTask;
        }
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

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
        && !string.IsNullOrWhiteSpace(headerValue)
        ? headerValue.ToString()
        : Guid.NewGuid().ToString("N");

    context.Items[CorrelationIdHeader] = correlationId;
    context.Response.Headers[CorrelationIdHeader] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var stopwatch = Stopwatch.StartNew();
    logger.LogInformation("Request started: {Method} {Path}", context.Request.Method, context.Request.Path);

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception during request pipeline for {Method} {Path}", context.Request.Method, context.Request.Path);
        throw;
    }
    finally
    {
        stopwatch.Stop();
        logger.LogInformation("Request completed: {Method} {Path} responded {StatusCode} in {ElapsedMs} ms", context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
    }
});

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AuditDbContext>();
        context.Database.Migrate();
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("AuditService: Database migrations applied successfully.");
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

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Audit Service API v1"));

// HTTPS redirection disabled for Render deployment (Render handles HTTPS at load balancer)
// app.UseHttpsRedirection();

app.UseCors("FrontendCors");

app.UseAuthentication();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var correlationId = httpContext.Items.TryGetValue(CorrelationIdHeader, out var correlationValue)
            ? correlationValue?.ToString() ?? httpContext.TraceIdentifier
            : httpContext.TraceIdentifier;

        var userId = httpContext.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
              ?? httpContext.User.FindFirst("sub")?.Value
              ?? "authenticated-user"
            : "anonymous";

        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("CorrelationId", correlationId);
        diagnosticContext.Set("UserId", userId);
    };
});

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

try
{
    Log.Information("Starting AuditService API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AuditService API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string CorrelationHeaderName = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(CorrelationHeaderName))
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var correlationId = httpContext?.Items.TryGetValue(CorrelationHeaderName, out var value) == true
                ? value?.ToString()
                : httpContext?.Request.Headers[CorrelationHeaderName].ToString();

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
            }

            request.Headers.TryAddWithoutValidation(CorrelationHeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
