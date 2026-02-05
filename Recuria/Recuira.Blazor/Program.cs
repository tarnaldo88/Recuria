using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Recuira.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
{
    var http = new HttpClient { BaseAddress = new Uri("http://localhost:5132") };
    return new Recuria.Client.RecuriaApiClient(http);
});

await builder.Build().RunAsync();
