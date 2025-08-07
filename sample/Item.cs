using System.Text.Json.Serialization;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB.Sample;

public class Item : IdItem, IIdItem<Item>
{
    /// <summary>
    /// The <see cref="IdItemTypeName"/> for this class.
    /// </summary>
    public new const string IIdItemTypeName = ":Item:";

    /// <summary>
    /// A built-in, read-only type discriminator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inheritance and polymorphism are modeled by chaining subtypes with the ':' character as a
    /// separator.
    /// </para>
    /// <para>
    /// For example: ":BaseType:ChildType:".
    /// </para>
    /// <para>
    /// Note that this property is expected to always return the same value as <see
    /// cref="IIdItem{TSelf}.GetIdItemTypeName"/> for this type.
    /// </para>
    /// </remarks>
    [JsonPropertyName("_id_t"), JsonInclude, JsonPropertyOrder(-1)]
    public override string IdItemTypeName { get => IIdItemTypeName; init { } }

    [JsonIgnore] public bool IsUpdating { get; set; }

    [JsonIgnore] public string? NewValue { get; set; }

    public string? Value { get; set; }

    /// <summary>
    /// Gets the <see cref="IdItemTypeName"/> for any instance of this class as a static method.
    /// </summary>
    /// <returns>The <see cref="IdItemTypeName"/> for any instance of this class.</returns>
    static string IIdItem.GetIdItemTypeName() => IIdItemTypeName;
}
