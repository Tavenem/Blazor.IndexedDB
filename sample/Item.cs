using System.Text.Json.Serialization;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB.Sample;

public class Item : IdItem
{
    public const string ItemTypeName = ":Item:";

    [JsonInclude]
    [JsonPropertyOrder(-1)]
    public override string IdItemTypeName
    {
        get => ItemTypeName;
        set { }
    }

    [JsonIgnore] public bool IsUpdating { get; set; }

    [JsonIgnore] public string? NewValue { get; set; }

    public string? Value { get; set; }
}
