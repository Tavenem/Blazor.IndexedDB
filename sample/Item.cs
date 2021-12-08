using System.Text.Json.Serialization;

namespace Tavenem.Blazor.IndexedDB.Sample;

public class Item
{
    public string? Id { get; set; }

    [JsonIgnore] public bool IsUpdating { get; set; }

    [JsonIgnore] public string? NewValue { get; set; }

    public string? Value { get; set; }
}
