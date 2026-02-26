using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Recuria.Blazor;
using Recuria.Blazor.Services;
using Recuria.Blazor.Services.App;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services; 

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<ApiCallRunner>();
builder.Services.AddScoped<IAuthAppService, AuthAppService>();
builder.Services.AddScoped<IBootstrapAppService, BootstrapAppService>();
builder.Services.AddScoped<IOrganizationAppService, OrganizationAppService>();
builder.Services.AddScoped<ISubscriptionAppService, SubscriptionAppService>();
builder.Services.AddScoped<IInvoiceAppService, InvoiceAppService>();
builder.Services.AddScoped<IUserAppService, UserAppService>();
builder.Services.AddScoped<IOpsAppService, OpsAppService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IPaymentAppService, PaymentAppService>();
builder.Services.AddMudServices();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5132/";

// One shared authenticated HttpClient for app services
builder.Services.AddScoped<HttpClient>(sp =>
{
    var handler = sp.GetRequiredService<AuthHeaderHandler>();
    if (handler.InnerHandler is null)
        handler.InnerHandler = new HttpClientHandler();

    return new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});

// NSwag client built from same HttpClient
builder.Services.AddScoped<Recuria.Client.RecuriaApiClient>(sp =>
    new Recuria.Client.RecuriaApiClient(sp.GetRequiredService<HttpClient>()));

builder.Services.AddScoped<Recuria.Client.IRecuriaApiClient>(sp =>
    sp.GetRequiredService<Recuria.Client.RecuriaApiClient>());


await builder.Build().RunAsync();