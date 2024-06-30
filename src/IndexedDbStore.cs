using System.Text.Json.Serialization.Metadata;
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
    IndexedDb database) : IDataStore, IAsyncDisposable
{
    private bool _disposed;

    /// <summary>
    /// The <see cref="IndexedDb"/> in which this store resides.
    /// </summary>
    public IndexedDb Database { get; init; } = database;

    /// <summary>
    /// Ignored. <see cref="IndexedDbService"/> does not cache results.
    /// </summary>
    public TimeSpan DefaultCacheTimeout { get; set; }

    /// <summary>
    /// The name of the object store.
    /// </summary>
    public string? StoreName { get; init; } = storeName;

    /// <summary>
    /// Indicates whether this <see cref="IDataStore"/> implementation allows items to be
    /// cached.
    /// </summary>
    /// <remarks>
    /// This is <see langword="false"/> for <see cref="IndexedDbService"/>.
    /// </remarks>
    public bool SupportsCaching => false;

    internal IndexedDbStoreInfo Info { get; init; } = new()
    {
        DatabaseName = database.DatabaseName,
        StoreName = storeName,
        Version = database.Version,
    };

    /// <summary>
    /// Clears the database.
    /// </summary>
    public Task ClearAsync() => Database.Service.ClearAsync(this);

    /// <summary>
    /// Retrieves the number of items in the database.
    /// </summary>
    public Task<long> CountAsync() => Database.Service.CountAsync(this);

    /// <summary>
    /// Creates a new <see cref="IIdItem.Id"/> for an <see cref="IIdItem"/> of the given type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of <see cref="IIdItem"/> for which to generate an id.
    /// </typeparam>
    /// <returns>
    /// A new <see cref="IIdItem.Id"/> for an <see cref="IIdItem"/> of the given type.
    /// </returns>
    /// <remarks>
    /// <see cref="IndexedDbService"/> uses a <see cref="Guid"/>.
    /// </remarks>
    public string CreateNewIdFor<T>() where T : class, IIdItem => Guid.NewGuid().ToString();

    /// <summary>
    /// Creates a new id for an item of the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The type for which to generate an id. Expected to be an instance of <see cref="IIdItem"/>,
    /// but should not throw an exception even if a different type is supplied.
    /// </param>
    /// <returns>A new id for an item of the given <paramref name="type"/>.</returns>
    /// <remarks>
    /// <see cref="IndexedDbService"/> uses a <see cref="Guid"/>.
    /// </remarks>
    public string CreateNewIdFor(Type type) => Guid.NewGuid().ToString();

    /// <summary>
    /// Disposes of the service and its resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        await Database.Service.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Retrieves all the items in an IndexedDB object store.
    /// </summary>
    /// <typeparam name="TValue">The type of value being retrieved.</typeparam>
    /// <remarks>
    /// Note: the IndexedDB object store cannot filter items by type. Using this method when there
    /// are objects of different types in your data store will result in an exception when
    /// attempting to deserialize items of types other than <typeparamref name="TValue"/>. This
    /// method should only be used when you only employ this object store to store objects of a uniform
    /// type (or which inherit from a common type).
    /// </remarks>
    public IAsyncEnumerable<TValue> GetAllAsync<TValue>(JsonTypeInfo<TValue>? typeInfo = null)
        => Database.Service.GetAllAsync(this, typeInfo);

    /// <summary>
    /// Retrieves a batch of items from an IndexedDB object store.
    /// </summary>
    /// <typeparam name="TValue">The type of value being retrieved.</typeparam>
    /// <param name="reset">
    /// <para>
    /// Whether to restart iteration from the beginning.
    /// </para>
    /// <para>
    /// When <see langword="false"/>, successive calls to this method will fetch batches of items
    /// from the store until no more items remain to be enumerated. At that point, all following
    /// calls will return an empty array, until <see langword="true"/> is passed for this parameter.
    /// </para>
    /// </param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="TValue"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method can be used directly, but it may be more intuitive to call <see
    /// cref="Query{T}"/> and one of the <see cref="IDataStoreQueryable{T}"/>
    /// methods, such as <see cref="IDataStoreQueryable{T}.AsAsyncEnumerable"/>, when enumerating
    /// items.
    /// </para>
    /// <para>
    /// Note: the IndexedDB object store cannot filter items by type. Using this method when there
    /// are objects of different types in your data store will result in an exception when
    /// attempting to deserialize items of types other than <typeparamref name="TValue"/>. This
    /// method should only be used when you only employ this database to store objects of a uniform
    /// type (or which inherit from a common type).
    /// </para>
    /// </remarks>
    public IAsyncEnumerable<TValue> GetBatchAsync<TValue>(bool reset = false, JsonTypeInfo<TValue>? typeInfo = null)
        => Database.Service.GetBatchAsync(this, reset, typeInfo);

    /// <summary>
    /// Gets the <see cref="IIdItem"/> with the given <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to retrieve.</typeparam>
    /// <param name="id">The unique id of the item to retrieve.</param>
    /// <returns>
    /// The item with the given id, or <see langword="null"/> if no item was found with that id.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This wraps <see cref="GetItemAsync{T}(string?)"/> in a <see cref="Task"/> and blocks on the
    /// result. Always use <see cref="GetItemAsync{T}(string?)"/> when possible.
    /// </para>
    /// <para>
    /// Note: this overload will typically fail in the browser (or whenever trimming is enabled),
    /// since it relies on reflection-based (de)serialization. To use source generated
    /// deserialization, use the overload which takes a <see cref="JsonTypeInfo{T}"/>.
    /// </para>
    /// </remarks>
    public T? GetItem<T>(string? id)
        => GetItemAsync<T>(id).AsTask().GetAwaiter().GetResult();

    T? IDataStore.GetItem<T>(string? id, TimeSpan? cacheTimeout) where T : class => GetItem<T>(id);

    /// <summary>
    /// Gets the <see cref="IIdItem"/> with the given <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to retrieve.</typeparam>
    /// <param name="id">The unique id of the item to retrieve.</param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// The item with the given id, or <see langword="null"/> if no item was found with that id.
    /// </returns>
    /// <remarks>
    /// This wraps <see cref="GetItemAsync{T}(string?, JsonTypeInfo{T}?)"/> in a <see cref="Task"/>
    /// and blocks on the result. Always use <see cref="GetItemAsync{T}(string?,
    /// JsonTypeInfo{T}?)"/> when possible.
    /// </remarks>
    public T? GetItem<T>(string? id, JsonTypeInfo<T>? typeInfo)
        => GetItemAsync<T>(id, typeInfo).AsTask().GetAwaiter().GetResult();

    T? IDataStore.GetItem<T>(string? id, JsonTypeInfo<T>? typeInfo, TimeSpan? cacheTimeout) where T : class
        => GetItem<T>(id, typeInfo);

    /// <summary>
    /// Gets the <see cref="IIdItem"/> with the given <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to retrieve.</typeparam>
    /// <param name="id">The unique id of the item to retrieve.</param>
    /// <returns>
    /// The item with the given id, or <see langword="null"/> if no item was found with that id.
    /// </returns>
    /// <remarks>
    /// Note: this overload will typically fail in the browser (or whenever trimming is enabled),
    /// since it relies on reflection-based (de)serialization. To use source generated
    /// deserialization, use the overload which takes a <see cref="JsonTypeInfo{T}"/>.
    /// </remarks>
    public async ValueTask<T?> GetItemAsync<T>(string? id) => id is null
        ? default
        : await Database.Service.GetItemAsync<T>(this, id);

    ValueTask<T?> IDataStore.GetItemAsync<T>(string? id, TimeSpan? cacheTimeout) where T : class
        => GetItemAsync<T>(id);

    /// <summary>
    /// Gets the <see cref="IIdItem"/> with the given <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to retrieve.</typeparam>
    /// <param name="id">The unique id of the item to retrieve.</param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// The item with the given id, or <see langword="null"/> if no item was found with that id.
    /// </returns>
    public async ValueTask<T?> GetItemAsync<T>(string? id, JsonTypeInfo<T>? typeInfo) => id is null
        ? default
        : await Database.Service.GetItemAsync<T>(this, id, typeInfo);

    ValueTask<T?> IDataStore.GetItemAsync<T>(string? id, JsonTypeInfo<T>? typeInfo, TimeSpan? cacheTimeout) where T : class
        => GetItemAsync<T>(id, typeInfo);

    /// <inheritdoc/>
    public IDataStoreQueryable<T> Query<T>(JsonTypeInfo<T>? typeInfo = null) => new IndexedDbQueryable<T>(this, typeInfo);

    IDataStoreQueryable<T> IDataStore.Query<T>(JsonTypeInfo<T>? typeInfo) => Query(typeInfo);

    /// <summary>
    /// Removes the stored item with the given id.
    /// </summary>
    /// <param name="id">
    /// <para>
    /// The id of the item to remove.
    /// </para>
    /// <para>
    /// If <see langword="null"/> or empty no operation takes place, and <see langword="true"/> is
    /// returned to indicate that there was no failure.
    /// </para>
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully removed; otherwise <see
    /// langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This blocks on the result of <see cref="RemoveItemAsync(string?)"/>. Always use <see
    /// cref="RemoveItemAsync(string?)"/> when possible.
    /// </remarks>
    public bool RemoveItem(string? id)
        => RemoveItemAsync(id).GetAwaiter().GetResult();

    bool IDataStore.RemoveItem<T>(string? id) => RemoveItem(id);

    /// <summary>
    /// Removes the given stored item.
    /// </summary>
    /// <typeparam name="T">The type of items to remove.</typeparam>
    /// <param name="item">
    /// <para>
    /// The item to remove.
    /// </para>
    /// <para>
    /// If <see langword="null"/> or empty no operation takes place, and <see langword="true"/> is
    /// returned to indicate that there was no failure.
    /// </para>
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully removed, or did not exist; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This blocks on the result of <see cref="RemoveItemAsync{T}(T)"/>. Always use <see
    /// cref="RemoveItemAsync{T}(T)"/> when possible.
    /// </remarks>
    public bool RemoveItem<T>(T? item) where T : class, IIdItem
        => RemoveItem(item?.Id);

    /// <summary>
    /// Removes the stored item with the given id.
    /// </summary>
    /// <param name="id">
    /// <para>
    /// The id of the item to remove.
    /// </para>
    /// <para>
    /// If <see langword="null"/> or empty no operation takes place, and <see langword="true"/> is
    /// returned to indicate that there was no failure.
    /// </para>
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully removed, or did not exist; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public async Task<bool> RemoveItemAsync(string? id) => id is null
        || await Database.Service.RemoveItemAsync(this, id);

    Task<bool> IDataStore.RemoveItemAsync<T>(string? id) where T : class => RemoveItemAsync(id);

    /// <inheritdoc/>
    public Task<bool> RemoveItemAsync<T>(T? item) where T : class, IIdItem
        => RemoveItemAsync(item?.Id);

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of object to upsert.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>, to
    /// indicate that the operation did not fail (even though no storage operation took place,
    /// neither did any failure).
    /// </remarks>
    /// <remarks>
    /// <para>
    /// This blocks on the result of <see cref="StoreAsync{T}(T)"/>. Always use <see
    /// cref="StoreAsync{T}(T)"/> when possible.
    /// </para>
    /// <para>
    /// Note: this overload will typically fail in the browser (or whenever trimming is enabled),
    /// since it relies on reflection-based (de)serialization. To use source generated
    /// deserialization, use the overload which takes a <see cref="JsonTypeInfo{T}"/>.
    /// </para>
    /// </remarks>
    public bool Store<T>(T? item) where T : class
        => StoreAsync(item).GetAwaiter().GetResult();

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of object to upsert.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>, to
    /// indicate that the operation did not fail (even though no storage operation took place,
    /// neither did any failure).
    /// </remarks>
    /// <remarks>
    /// This blocks on the result of <see cref="StoreAsync{T}(T)"/>. Always use <see
    /// cref="StoreAsync{T}(T, JsonTypeInfo{T}?)"/> when possible.
    /// </remarks>
    public bool Store<T>(T? item, JsonTypeInfo<T>? typeInfo) where T : class
        => StoreAsync(item, typeInfo).GetAwaiter().GetResult();

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of object to upsert.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>, to
    /// indicate that the operation did not fail (even though no storage operation took place,
    /// neither did any failure).
    /// </para>
    /// <para>
    /// Note: this differs from <see cref="StoreItemAsync{T}(T)"/> in that <see
    /// cref="StoreItemAsync{T}(T)"/> expects that <paramref name="item"/> implements <see
    /// cref="IIdItem"/>, and serializes it accordingly.
    /// </para>
    /// </remarks>
    public async Task<bool> StoreAsync<T>(T? item) where T : class
        => item is null || await Database.Service.StoreAsync(this, item);

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of object to upsert.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>, to
    /// indicate that the operation did not fail (even though no storage operation took place,
    /// neither did any failure).
    /// </para>
    /// <para>
    /// Note: this differs from <see cref="StoreItemAsync{T}(T, JsonTypeInfo{T})"/> in that <see
    /// cref="StoreItemAsync{T}(T, JsonTypeInfo{T})"/> expects that <paramref name="item"/>
    /// implements <see cref="IIdItem"/>, and serializes it accordingly.
    /// </para>
    /// </remarks>
    public async Task<bool> StoreAsync<T>(T? item, JsonTypeInfo<T>? typeInfo) where T : class
        => item is null || await Database.Service.StoreAsync<T>(this, item, typeInfo);

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to upsert.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>, to
    /// indicate that the operation did not fail (even though no storage operation took place,
    /// neither did any failure).
    /// </remarks>
    /// <remarks>
    /// <para>
    /// This blocks on the result of <see cref="StoreItemAsync{T}(T)"/>. Always use <see
    /// cref="StoreItemAsync{T}(T)"/> when possible.
    /// </para>
    /// <para>
    /// Note: this overload will typically fail in the browser (or whenever trimming is enabled),
    /// since it relies on reflection-based (de)serialization. To use source generated
    /// deserialization, use the overload which takes a <see cref="JsonTypeInfo{T}"/>.
    /// </para>
    /// </remarks>
    public bool StoreItem<T>(T? item) where T : class, IIdItem
        => StoreItemAsync(item).GetAwaiter().GetResult();

    bool IDataStore.StoreItem<T>(T? item, TimeSpan? cacheTimeout) where T : class
        => StoreItem(item);

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to upsert.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>, to
    /// indicate that the operation did not fail (even though no storage operation took place,
    /// neither did any failure).
    /// </remarks>
    /// <remarks>
    /// This blocks on the result of <see cref="StoreItemAsync{T}(T)"/>. Always use <see
    /// cref="StoreItemAsync{T}(T, JsonTypeInfo{T}?)"/> when possible.
    /// </remarks>
    public bool StoreItem<T>(T? item, JsonTypeInfo<T>? typeInfo) where T : class, IIdItem
        => StoreItemAsync(item, typeInfo).GetAwaiter().GetResult();

    bool IDataStore.StoreItem<T>(T? item, JsonTypeInfo<T>? typeInfo, TimeSpan? cacheTimeout) where T : class
        => StoreItem(item);

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to upsert.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>, to
    /// indicate that the operation did not fail (even though no storage operation took place,
    /// neither did any failure).
    /// </para>
    /// <para>
    /// Note: this differs from <see cref="StoreAsync{T}(T)"/> in that this method expects that
    /// <paramref name="item"/> implements <see cref="IIdItem"/>, and serializes it accordingly,
    /// whereas <see cref="StoreAsync{T}(T)"/> does not.
    /// </para>
    /// </remarks>
    public async Task<bool> StoreItemAsync<T>(T? item) where T : class, IIdItem
        => item is null || await Database.Service.StoreItemAsync(this, item);

    Task<bool> IDataStore.StoreItemAsync<T>(T? item, TimeSpan? cacheTimeout) where T : class
        => StoreItemAsync(item);

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to upsert.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>, to
    /// indicate that the operation did not fail (even though no storage operation took place,
    /// neither did any failure).
    /// </para>
    /// <para>
    /// Note: this differs from <see cref="StoreAsync{T}(T, JsonTypeInfo{T})"/> in that this method
    /// expects that <paramref name="item"/> implements <see cref="IIdItem"/>, and serializes it
    /// accordingly, whereas <see cref="StoreAsync{T}(T, JsonTypeInfo{T})"/> does not.
    /// </para>
    /// </remarks>
    public async Task<bool> StoreItemAsync<T>(T? item, JsonTypeInfo<T>? typeInfo) where T : class, IIdItem
        => item is null || await Database.Service.StoreItemAsync<T>(this, item, typeInfo);

    Task<bool> IDataStore.StoreItemAsync<T>(T? item, JsonTypeInfo<T>? typeInfo, TimeSpan? cacheTimeout) where T : class
        => StoreItemAsync(item, typeInfo);
}
