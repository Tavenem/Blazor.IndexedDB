using System.Text.Json.Serialization;
using Tavenem.Blazor.IndexedDB.Sample.Models;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB.Sample.Entities;

public class EntityBase
    : IdItem
{
    public string ItemTypeName => this.GetType().Name;

    [JsonInclude]
    [JsonPropertyOrder(-1)]
    public override string IdItemTypeName
    {
        get => ItemTypeName;
        set { }
    }

    [JsonIgnore]
    public bool IsEditing { get; set; }
}
