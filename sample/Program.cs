using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Tavenem.Blazor.IndexedDB;
using Tavenem.Blazor.IndexedDB.Sample;
using Tavenem.DataStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
options.TypeInfoResolverChain.Add(ItemContext.Default.WithAddedModifier(static typeInfo =>
{
    if (typeInfo.Type == typeof(IIdItem))
    {
        typeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions
        {
            IgnoreUnrecognizedTypeDiscriminators = true,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor,
        };
        typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(typeof(Item), Item.ItemTypeName));
    }
}));

builder.Services.AddIndexedDb(
    new IndexedDb("Tavenem.Blazor.IndexedDB.Sample", 1),
    options);

await builder.Build().RunAsync().ConfigureAwait(false);
