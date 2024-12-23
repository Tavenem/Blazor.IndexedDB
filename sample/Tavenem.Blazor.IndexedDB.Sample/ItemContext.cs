using System.Text.Json.Serialization;
using Tavenem.Blazor.IndexedDB.Sample.Models;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB.Sample;


[JsonSerializable(typeof(IIdItem))]
[JsonSerializable(typeof(Item))]
[JsonSerializable(typeof(Person))]
public partial class DatabaseContext : JsonSerializerContext
{
    public const string DatabaseName = "Tavenem.Blazor.IndexedDB.Sample";
}
