using System.Text.Json;
using System.Text.Json.Serialization;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB.Sample;

[JsonSerializable(typeof(IIdItem))]
[JsonSerializable(typeof(Item))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
public partial class ItemContext : JsonSerializerContext;
