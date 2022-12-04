using System.Text.Json.Serialization;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB.Sample;

public class Item : IdItem
{
    [JsonIgnore] public bool IsUpdating { get; set; }

    [JsonIgnore] public string? NewValue { get; set; }

    public string? Value { get; set; }
}
