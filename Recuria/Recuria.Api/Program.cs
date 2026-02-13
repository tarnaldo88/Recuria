using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Recuria.Api.Middleware;
using Recuria.Api.Auth;
using Recuria.Api.Configuration;
using Recuria.Api.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Recuria.Application;
using Recuria.Application.Contracts.Invoice.Validators;
using Recuria.Application.Contracts.Organizations.Validators;
using Recuria.Application.Contracts.Subscription.Validators;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Interface.Idempotency;
using Recuria.Application.Observability;
using Recuria.Application.Subscriptions;
using Recuria.Application.Validation;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Events.Subscription;
using Recuria.Domain.Enums;
using Recuria.Infrastructure;
using Recuria.Infrastructure.Idempotency;
using Recuria.Infrastructure.Observability;
using Recuria.Infrastructure.Outbox;
using Recuria.Infrastructure.Persistence;
using Recuria.Infrastructure.Persistence.Locking;
using Recuria.Infrastructure.Persistence.Queries;
using Recuria.Infrastructure.Repositories;
using Recuria.Infrastructure.Subscriptions;
using System.Text.Json.Serialization;
using System.Text;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;
using Recuria.Api.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});

builder.Host.UseSerilog((ctx, lc) =>
{
    var env = ctx.HostingEnvironment;
    var minLevel = env.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information;

    lc.MinimumLevel.Is(minLevel)
      .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
      .MinimumLevel.Override("System", LogEventLevel.Warning)
      .Enrich.FromLogContext()
      .Enrich.WithProperty("Application", "Recuria")
      .Enrich.WithProperty("Environment", env.EnvironmentName)
      .WriteTo.Console(new CompactJsonFormatter());
});

var requireJwt = !builder.Environment.IsDevelopment();
if (requireJwt)
{
    var issuer = builder.Configuration["Jwt:Issuer"];
    var audience = builder.Configuration["Jwt:Audience"];
    var key = builder.Configuration["Jwt:SigningKey"];
    var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

    if (string.IsNullOrWhiteSpace(issuer) ||
        string.IsNullOrWhiteSpace(audience) ||
        string.IsNullOrWhiteSpace(key))
    {
        throw new InvalidOperationException(
            "JWT configuration is required in non-development environments (Jwt:Issuer, Jwt:Audience, Jwt:SigningKey).");
    }

    if (key.Length < 32)
    {
        throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters in non-development environments.");
    }

    if (string.IsNullOrWhiteSpace(defaultConnection) ||
        defaultConnection.Contains("(localdb)", StringComparison.OrdinalIgnoreCase) ||
        defaultConnection.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("A non-local production database connection string is required in non-development environments.");
    }

    if (allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin in non-development environments.");
    }
}

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o =>
        !string.IsNullOrWhiteSpace(o.Issuer) &&
        !string.IsNullOrWhiteSpace(o.Audience) &&
        !string.IsNullOrWhiteSpace(o.SigningKey),
        "Jwt:Issuer, Jwt:Audience, and Jwt:SigningKey are required.")
    .ValidateOnStart();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

builder.Services.AddMemoryCache();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed.",
            Type = "https://httpstatuses.com/400",
            Instance = context.HttpContext.Request.Path
        };

        details.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        details.Extensions["errorCode"] = "validation_error";
        return new BadRequestObjectResult(details)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<RecuriaDbContext>("db", failureStatus: HealthStatus.Unhealthy);

builder.Services.AddSingleton<IAuditLogger, AuditLogger>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("default", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth-login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{httpContext.Connection.RemoteIpAddress}:{httpContext.Request.Path}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Recuria API",
        Version = "v1"
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename), includeControllerXmlComments: true);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
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

    options.OperationFilter<IdempotencyHeaderOperationFilter>();
});

builder.Services.AddDbContext<RecuriaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.CommandTimeout(10)));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var issuer = jwt.Issuer;
        var audience = jwt.Audience;
        var key = jwt.SigningKey;
        var requireJwt = !builder.Environment.IsDevelopment();

        options.RequireHttpsMetadata = requireJwt;
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = requireJwt || !string.IsNullOrWhiteSpace(issuer),
            ValidateAudience = requireJwt || !string.IsNullOrWhiteSpace(audience),
            ValidateIssuerSigningKey = requireJwt || !string.IsNullOrWhiteSpace(key),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidIssuer = issuer,
            ValidAudience = audience,
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKey = string.IsNullOrWhiteSpace(key)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                if (principal is null)
                {
                    context.Fail("Missing principal.");
                    return;
                }

                var tokenVersionRaw = principal.FindFirstValue("token_version");
                if (string.IsNullOrWhiteSpace(tokenVersionRaw))
                    return;

                var userIdRaw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                                ?? principal.FindFirstValue("sub");

                if (!Guid.TryParse(userIdRaw, out var userId) ||
                    !int.TryParse(tokenVersionRaw, out var tokenVersion))
                {
                    context.Fail("Invalid token claims.");
                    return;
                }

                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = await users.GetByIdAsync(userId, context.HttpContext.RequestAborted);
                if (user is null || user.TokenVersion != tokenVersion)
                    context.Fail("Token is revoked.");
            }
        };

    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("OwnerOnly", policy =>
        policy.RequireRole(UserRole.Owner.ToString()));

    options.AddPolicy("AdminOrOwner", policy =>
        policy.RequireRole(UserRole.Admin.ToString(), UserRole.Owner.ToString()));

    options.AddPolicy("MemberOrAbove", policy =>
        policy.RequireRole(
            UserRole.Member.ToString(),
            UserRole.Admin.ToString(),
            UserRole.Owner.ToString()));
});

builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IOrganizationQueries, OrganizationQueries>();
builder.Services.AddScoped<ISubscriptionQueries, SubscriptionQueries>();
builder.Services.AddScoped<IInvoiceQueries, InvoiceQueries>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<OutboxProcessor>();
builder.Services.AddHostedService<OutboxProcessorHostedService>();
builder.Services.AddScoped<IDatabaseDistributedLock, SqlServerDistributedLock>();
builder.Services.AddLogging();
builder.Services.AddMetrics();
builder.Services.AddScoped<ISubscriptionTelemetry, SubscriptionTelemetry>();
//builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssembly(typeof(CreateOrganizationRequestValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(UpgradeSubscriptionRequestValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateInvoiceRequestValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(AddUserRequestValidator).Assembly);
builder.Services.AddScoped<ValidationBehavior>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IBillingRetryPolicy, ExponentialBackoffRetryPolicy>();
builder.Services.AddScoped<ISubscriptionLifecycleOrchestrator, SubscriptionLifecycleOrchestrator>();
builder.Services.AddScoped<SubscriptionLifecycleProcessor>();
builder.Services.AddHostedService<SubscriptionLifecycleHostedService>();

builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProcessedEventStore, EfProcessedEventStore>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IApiIdempotencyStore, EfApiIdempotencyStore>();
//builder.Services.AddScoped<IDomainEventHandler<SubscriptionActivatedDomainEvent>, SubscriptionActivatedHandler>();

builder.Services.AddOptions<IdempotencyOptions>()
    .Bind(builder.Configuration.GetSection(IdempotencyOptions.SectionName))
    .Validate(o => o.InvoiceCreateTtlHours > 0 && o.InvoiceCreateTtlHours <= 168,
        "Idempotency.InvoiceCreateTtlHours must be between 1 and 168.")
    .ValidateOnStart();

builder.Services.Scan(scan => scan
    .FromAssemblies(
        typeof(IDomainEventHandler<>).Assembly,
        typeof(SubscriptionActivatedHandler).Assembly)
    .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService("Recuria"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Recuria")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Recuria")
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter();
    });


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    // app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
    var csp = builder.Configuration["SecurityHeaders:ContentSecurityPolicy"];
    if (!string.IsNullOrWhiteSpace(csp))
    {
        context.Response.Headers["Content-Security-Policy"] = csp;
    }
    var permissions = builder.Configuration["SecurityHeaders:PermissionsPolicy"];
    if (!string.IsNullOrWhiteSpace(permissions))
    {
        context.Response.Headers["Permissions-Policy"] = permissions;
    }
    await next();
});

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Recuria API v1");
        options.RoutePrefix = "swagger";
        options.EnablePersistAuthorization();
        options.EnableTryItOutByDefault(); 
    });
}

app.UseCors("DefaultCors");

app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.HasStarted || response.ContentLength > 0 || !string.IsNullOrEmpty(response.ContentType))
        return;

    var status = response.StatusCode;
    if (status < 400)
        return;

    var details = new ProblemDetails
    {
        Status = status,
        Title = status switch
        {
            StatusCodes.Status401Unauthorized => "Unauthorized.",
            StatusCodes.Status403Forbidden => "Forbidden.",
            StatusCodes.Status404NotFound => "Not found.",
            StatusCodes.Status429TooManyRequests => "Too many requests.",
            _ => "Request failed."
        },
        Type = $"https://httpstatuses.com/{status}",
        Instance = context.HttpContext.Request.Path
    };

    details.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    if (context.HttpContext.Response.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
        details.Extensions["correlationId"] = correlationId.ToString();
    details.Extensions["errorCode"] = status switch
    {
        StatusCodes.Status401Unauthorized => "auth_required",
        StatusCodes.Status403Forbidden => "forbidden",
        StatusCodes.Status404NotFound => "not_found",
        StatusCodes.Status429TooManyRequests => "rate_limited",
        _ => "request_failed"
    };

    response.ContentType = "application/problem+json";
    await response.WriteAsJsonAsync(details);
});

app.MapControllers().RequireRateLimiting("default");

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "db"
}).AllowAnonymous();

app.UseOpenTelemetryPrometheusScrapingEndpoint();


app.Run();

namespace Recuria.Api
{
    public partial class Program { }
}
