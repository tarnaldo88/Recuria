using Microsoft.EntityFrameworkCore;
using Recuria.Application.Interface;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Events;
using Recuria.Infrastructure;
using Recuria.Infrastructure.Outbox;
using Recuria.Infrastructure.Persistence;
using Recuria.Infrastructure.Repositories;
using Scrutor;

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

builder.Services.Scan(scan => scan
    .FromAssembliesOf(typeof(IDomainEventHandler<>))
    .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
