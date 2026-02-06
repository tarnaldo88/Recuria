using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Recuira.Blazor;
using Recuira.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<AuthState>();

builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri("http://localhost:5132/");
})
.AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthHeaderHandler>();
    var http = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost:5132/")
    };
    return new Recuria.Client.RecuriaApiClient(http);
});

await builder.Build().RunAsync();
