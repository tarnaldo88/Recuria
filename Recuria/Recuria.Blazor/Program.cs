using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Recuria.Blazor;
using Recuria.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services; 

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddMudServices();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5132/";

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthHeaderHandler>();
    handler.InnerHandler = new HttpClientHandler();
    var http = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
    return new Recuria.Client.RecuriaApiClient(http);
});

builder.Services.AddScoped<Recuria.Client.IRecuriaApiClient>(sp =>
    sp.GetRequiredService<Recuria.Client.RecuriaApiClient>());

await builder.Build().RunAsync();
