using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Information about an IndexedDB object store.
/// </summary>
/// <param name="storeName">
/// The name of the object store.
/// </param>
/// <param name="database">
/// The <see cref="IndexedDb"/> in which this store resides.
/// </param>
/// <remarks>
/// See <a
/// href="https://developer.mozilla.org/en-US/docs/Web/API/IDBObjectStore">https://developer.mozilla.org/en-US/docs/Web/API/IDBObjectStore</a>
/// </remarks>
public class IndexedDbStore(
    string storeName,
    IndexedDb database) : IndexedDbStore<IIdItem>(storeName, database), IIdItemDataStore
{
    /// <inheritdoc />
    public override string GetKey<T>(T item) => item.Id;

    /// <summary>
    /// Gets the name of the property used to discriminate types, if any.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <returns>
    /// The name of the property used to discriminate types, if any.
    /// </returns>
    /// <remarks>
    /// Always returns <see cref="IIdItem.IdItemTypePropertyName"/> for <see cref="IndexedDbStore"/>.
    /// </remarks>
    public override string? GetTypeDiscriminatorName<T>() => IIdItem.IdItemTypePropertyName;

    /// <summary>
    /// Gets the name of the property used to discriminate types, if any.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="item">The item whose discriminator property is being obtained.</param>
    /// <returns>
    /// The name of the property used to discriminate types, if any.
    /// </returns>
    /// <remarks>
    /// Always returns <see cref="IIdItem.IdItemTypePropertyName"/> for <see cref="IndexedDbStore"/>.
    /// </remarks>
    public override string? GetTypeDiscriminatorName<T>(T item) => GetTypeDiscriminatorName<T>();

    /// <summary>
    /// Gets the value of the item's type discriminator, if any.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <returns>
    /// The value of <typeparamref name="T"/>'s type discriminator, if any.
    /// </returns>
    /// <remarks>
    /// Always returns <see cref="IIdItem.GetIdItemTypeName"/> for <see cref="IndexedDbStore"/>.
    /// </remarks>
    public override string? GetTypeDiscriminatorValue<T>() => T.GetIdItemTypeName();

    /// <summary>
    /// Gets the value of the item's type discriminator, if any.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="item">The item whose type discriminator is being obtained.</param>
    /// <returns>
    /// The value of <paramref name="item"/>'s type discriminator, if any.
    /// </returns>
    /// <remarks>
    /// Always returns <see cref="IIdItem.GetIdItemTypeName"/> for <see cref="IndexedDbStore"/>.
    /// </remarks>
    public override string? GetTypeDiscriminatorValue<T>(T item) => GetTypeDiscriminatorValue<T>();

    /// <summary>
    /// Gets an <see cref="IndexedDbQueryable{TItem,T}"/> for <see cref="IIdItem"/>.
    /// </summary>
    /// <returns>An <see cref="IndexedDbQueryable{TItem,T}"/> for <see cref="IIdItem"/>.</returns>
    public IndexedDbQueryable<IIdItem, IIdItem> Query() => new(this);
}
