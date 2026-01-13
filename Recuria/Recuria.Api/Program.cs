using Microsoft.EntityFrameworkCore;
using Recuria.Application.Interface;
using Recuria.Application.Subscriptions;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Events;
using Recuria.Infrastructure;
using Recuria.Infrastructure.Outbox;
using Recuria.Infrastructure.Persistence;
using Recuria.Infrastructure.Persistence.Locking;
using Recuria.Infrastructure.Repositories;
using Scrutor;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Recuria.Infrastructure.Observability;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<RecuriaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<OutboxProcessor>();
builder.Services.AddHostedService<OutboxProcessorHostedService>();
builder.Services.AddScoped<IDatabaseDistributedLock, SqlServerDistributedLock>();
builder.Services.AddLogging();
builder.Services.AddMetrics();

builder.Services.Scan(scan => scan
    .FromAssemblies(
        typeof(IDomainEventHandler<>).Assembly,
        typeof(SubscriptionActivatedHandler).Assembly)
    .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(Telemetry.ServiceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Telemetry.ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(Telemetry.ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter();
    });


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseOpenTelemetryPrometheusScrapingEndpoint();


app.Run();
