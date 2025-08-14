using System.Text.Json.Serialization.Metadata;
using Tavenem.DataStorage;
using Tavenem.DataStorage.Interfaces;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Information about an IndexedDB object store.
/// </summary>
/// <typeparam name="TItem">A shared interface for all stored items.</typeparam>
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
public abstract class IndexedDbStore<TItem>(
    string storeName,
    IndexedDb database) : IDataStore<string, TItem>, IAsyncDisposable
    where TItem : notnull
{
    /// <summary>
    /// The <see cref="IndexedDb"/> in which this store resides.
    /// </summary>
    public IndexedDb Database { get; init; } = database;

    /// <summary>
    /// <para>
    /// Sets the default period after which cached items are considered stale.
    /// </para>
    /// <para>
    /// This is left at the default value for <see cref="IndexedDbStore{TItem}"/>, which does
    /// not support caching.
    /// </para>
    /// </summary>
    public TimeSpan DefaultCacheTimeout { get; set; }

    /// <summary>
    /// The name of the object store.
    /// </summary>
    public string? StoreName { get; init; } = storeName;

    /// <summary>
    /// <para>
    /// Indicates whether this <see cref="IDataStore"/> implementation allows items to be
    /// cached.
    /// </para>
    /// <para>
    /// This is <see langword="false"/> for <see cref="IndexedDbStore{TItem}"/>.
    /// </para>
    /// </summary>
    public bool SupportsCaching => false;

    internal IndexedDbStoreInfo Info { get; init; } = new()
    {
        DatabaseName = database.DatabaseName,
        StoreName = storeName,
        Version = database.Version,
        StoreNames = [.. database.ObjectStoreNames],
        KeyPath = database.Key,
    };

    /// <summary>
    /// Clears the database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    public Task ClearAsync(CancellationToken cancellationToken = default) => Database.Service.ClearAsync(this, cancellationToken);

    /// <summary>
    /// Retrieves the number of items in the database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    public Task<long> CountAsync(CancellationToken cancellationToken = default) => Database.Service.CountAsync(this, cancellationToken);

    /// <inheritdoc />
    public string? CreateNewIdFor<T>() where T : TItem => Guid.NewGuid().ToString();

    /// <inheritdoc />
    public string? CreateNewIdFor(Type type) => Guid.NewGuid().ToString();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Retrieves all the items in an IndexedDB object store.
    /// </summary>
    /// <typeparam name="TValue">The type of value being retrieved.</typeparam>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="TValue"/>.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <remarks>
    /// Note: the IndexedDB object store cannot filter items by type. Using this method when there
    /// are objects of different types in your data store will result in an exception when
    /// attempting to deserialize items of types other than <typeparamref name="TValue"/>. This
    /// method should only be used when you only employ this object store to store objects of a uniform
    /// type (or which inherit from a common type).
    /// </remarks>
    public IAsyncEnumerable<TValue> GetAllAsync<TValue>(JsonTypeInfo<TValue>? typeInfo = null, CancellationToken cancellationToken = default) where TValue : TItem
        => Database.Service.GetAllAsync(this, typeInfo, cancellationToken);

    /// <summary>
    /// Iterates all items from an IndexedDB object store.
    /// </summary>
    /// <typeparam name="TValue">The type of object to retrieve.</typeparam>
    /// <param name="skip">
    /// The number of items to skip. Optional.
    /// </param>
    /// <param name="take">
    /// The maximum number of items to take. Optional.
    /// </param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="TValue"/>.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <remarks>
    /// <para>
    /// This method can be used directly, but it may be more intuitive to call
    /// <c>IndexedDbStore{TItem}.Query{T}</c>  when enumerating items.
    /// </para>
    /// <para>
    /// Note: the IndexedDB object store cannot filter items by type. Using this method when there
    /// are objects of different types in your data store will result in an exception when
    /// attempting to deserialize items of types other than <typeparamref name="TValue"/>. This
    /// method should only be used when you only employ this database to store objects of a uniform
    /// type (or which inherit from a common type).
    /// </para>
    /// </remarks>
    public IAsyncEnumerable<TValue> GetAllBatchesAsync<TValue>(
        int? skip = null,
        int? take = null,
        JsonTypeInfo<TValue>? typeInfo = null,
        CancellationToken cancellationToken = default) where TValue : TItem => Database.Service.GetAllBatchesAsync(
            this,
            skip,
            take,
            GetTypeDiscriminatorName<TValue>(),
            GetTypeDiscriminatorValue<TValue>(),
            typeInfo,
            cancellationToken);

    /// <summary>
    /// Retrieves a batch of items from an IndexedDB object store.
    /// </summary>
    /// <typeparam name="TValue">The type of value being retrieved.</typeparam>
    /// <param name="skip">
    /// <para>
    /// The number of items to skip. Optional.
    /// </para>
    /// <para>
    /// Should normally be set only when fetching the first batch (i.e. when <paramref
    /// name="continuationKey"/> is <see langword="null"/>), otherwise the first <paramref
    /// name="skip"/> items in each batch will be skipped, which is not usually the desired
    /// behavior.
    /// </para>
    /// </param>
    /// <param name="take">
    /// The maximum number of items to take. Optional.
    /// </param>
    /// <param name="continuationKey">
    /// A continuation key (from the return value of a previous call to this method).
    /// </param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="TValue"/>.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// A list of results for this batch, and a continuation key for the next batch, if there are more results.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method can be used directly, but it may be more intuitive to call <c>Query{T}</c> when
    /// enumerating items.
    /// </para>
    /// <para>
    /// Note: the IndexedDB object store cannot filter items by type. Using this method when there
    /// are objects of different types in your data store will result in an exception when
    /// attempting to deserialize items of types other than <typeparamref name="TValue"/>. This
    /// method should only be used when you only employ this database to store objects of a uniform
    /// type (or which inherit from a common type).
    /// </para>
    /// </remarks>
    public Task<(List<TValue> Items, string? ContinuationKey)> GetBatchAsync<TValue>(
        int? skip = null,
        int? take = null,
        string? continuationKey = null,
        JsonTypeInfo<TValue>? typeInfo = null,
        CancellationToken cancellationToken = default) where TValue : TItem => Database.Service.GetBatchAsync(
            this,
            skip,
            take,
            GetTypeDiscriminatorName<TValue>(),
            GetTypeDiscriminatorValue<TValue>(),
            continuationKey,
            typeInfo,
            cancellationToken);

    /// <inheritdoc />
    public abstract string GetKey<T>(T item) where T : TItem;

    /// <inheritdoc />
    public ValueTask<T?> GetItemAsync<T>(string? id, TimeSpan? cacheTimeout = null, CancellationToken cancellationToken = default) where T : TItem
        => id is null
        ? ValueTask.FromResult<T?>(default)
        : Database.Service.GetItemAsync<TItem, T>(this, id, cancellationToken);

    /// <inheritdoc />
    public ValueTask<T?> GetItemAsync<T>(string? id, JsonTypeInfo<T>? typeInfo, TimeSpan? cacheTimeout = null, CancellationToken cancellationToken = default) where T : TItem
        => id is null
        ? ValueTask.FromResult<T?>(default)
        : Database.Service.GetItemAsync(this, id, typeInfo, cancellationToken);

    /// <summary>
    /// Gets the name of the property used to discriminate types, if any.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <returns>
    /// The name of the property used to discriminate types, if any.
    /// </returns>
    public abstract string? GetTypeDiscriminatorName<T>() where T : TItem;

    /// <summary>
    /// Gets the name of the property used to discriminate types, if any.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="item">The item whose discriminator property is being obtained.</param>
    /// <returns>
    /// The name of the property used to discriminate types, if any.
    /// </returns>
    public abstract string? GetTypeDiscriminatorName<T>(T item) where T : TItem;

    /// <summary>
    /// Gets the value of the item's type discriminator, if any.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <returns>
    /// The value of <typeparamref name="T"/>'s type discriminator, if any.
    /// </returns>
    public abstract string? GetTypeDiscriminatorValue<T>() where T : TItem;

    /// <summary>
    /// Gets the value of the item's type discriminator, if any.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="item">The item whose type discriminator is being obtained.</param>
    /// <returns>
    /// The value of <paramref name="item"/>'s type discriminator, if any.
    /// </returns>
    public abstract string? GetTypeDiscriminatorValue<T>(T item) where T : TItem;

    /// <inheritdoc />
    public IDataStoreQueryable<T> Query<T>(JsonTypeInfo<T>? typeInfo = null) where T : TItem
        => new IndexedDbQueryable<TItem, T>(this, typeInfo);

    /// <inheritdoc />
    public ValueTask<bool> RemoveItemAsync<T>(string? id, CancellationToken cancellationToken = default) where T : TItem
        => id is null
        ? ValueTask.FromResult(true)
        : Database.Service.RemoveItemAsync(this, id, cancellationToken);

    /// <inheritdoc />
    public ValueTask<bool> RemoveItemAsync<T>(T? item, CancellationToken cancellationToken = default) where T : TItem
        => item is null
        ? ValueTask.FromResult(true)
        : Database.Service.RemoveItemAsync(this, GetKey(item), cancellationToken);

    /// <inheritdoc />
    public ValueTask<T?> StoreItemAsync<T>(T? item, TimeSpan? cacheTimeout = null, CancellationToken cancellationToken = default) where T : TItem
        => item is null
        ? ValueTask.FromResult(item)
        : Database.Service.StoreAsync(this, item, cancellationToken);

    /// <inheritdoc />
    public ValueTask<T?> StoreItemAsync<T>(
        T? item,
        JsonTypeInfo<T>? typeInfo,
        TimeSpan? cacheTimeout = null,
        CancellationToken cancellationToken = default) where T : TItem
        => item is null
        ? ValueTask.FromResult(item)
        : Database.Service.StoreAsync(this, item, typeInfo, cancellationToken);

    /// <summary>
    /// Disposes of the service and its resources.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore() => await Database.Service.DisposeAsync().ConfigureAwait(false);
}
