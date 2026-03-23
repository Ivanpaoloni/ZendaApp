using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Zenda.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ELIMINÁ las líneas anteriores de HttpClient y dejá solo esta:
builder.Services.AddScoped(sp => new HttpClient
{
    // Puerto real donde corre tu API según me pasaste:
    //prod
    BaseAddress = new Uri("https://zenda-api.onrender.com/")
    
    //BaseAddress = new Uri("http://localhost:5039/")
});

await builder.Build().RunAsync();