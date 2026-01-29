using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Recuria.Api.Middleware;
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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<RecuriaDbContext>(options =>
    options.UseSqlServer( builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var key = builder.Configuration["Jwt:SigningKey"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            ValidateIssuerSigningKey = !string.IsNullOrWhiteSpace(key),
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKey = string.IsNullOrWhiteSpace(key)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddAuthorization(options =>
{
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
//builder.Services.AddScoped<IDomainEventHandler<SubscriptionActivatedDomainEvent>, SubscriptionActivatedHandler>();



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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseMiddleware<ErrorHandlingMiddleware>();


app.Run();

namespace Recuria.Api
{
    public partial class Program { }
}
