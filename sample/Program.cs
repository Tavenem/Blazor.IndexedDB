using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tavenem.Blazor.IndexedDB;
using Tavenem.Blazor.IndexedDB.Sample;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddIndexedDb(
    new IndexedDb<string>("Tavenem.Blazor.IndexedDB.Sample", 1));

await builder.Build().RunAsync().ConfigureAwait(false);
