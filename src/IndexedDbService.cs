using Microsoft.JSInterop;
using System.Text.Json;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Provides access to the IndexedDB API for a specific database.
/// </summary>
/// <param name="jsRuntime">An <see cref="IJSRuntime"/> instance.</param>
/// <param name="database">An <see cref="IndexedDb"/> instance.</param>
/// <param name="jsonSerializerOptions">A configured <see cref="JsonSerializerOptions"/> instance. Optional.</param>
public class IndexedDbService(
    IJSRuntime jsRuntime,
    IndexedDb database,
    JsonSerializerOptions? jsonSerializerOptions = null) : IDataStore, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask
        = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Tavenem.Blazor.IndexedDB/tavenem-indexeddb.js").AsTask());

    private bool _disposed;

    /// <summary>
    /// Ignored. <see cref="IndexedDbService"/> does not cache results.
    /// </summary>
    public TimeSpan DefaultCacheTimeout { get; set; }

    /// <summary>
    /// Indicates whether this <see cref="IDataStore"/> implementation allows items to be
    /// cached.
    /// </summary>
    /// <remarks>
    /// This is <see langword="false"/> for <see cref="IndexedDbService"/>.
    /// </remarks>
    public bool SupportsCaching => false;

    /// <summary>
    /// Clears the database.
    /// </summary>
    public async Task ClearAsync()
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("clear", database)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the number of items in the database.
    /// </summary>
    public async Task<long> CountAsync()
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<long>("count", database)
            .ConfigureAwait(false);
    }

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
    /// Deletes the database.
    /// </summary>
    /// <remarks>
    /// Note: this may not take effect immediately, or at all, if there are open
    /// connections to the database.
    /// </remarks>
    public async Task DeleteDatabaseAsync()
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("deleteDatabase", database.DatabaseName)
            .ConfigureAwait(false);
    }

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
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value.ConfigureAwait(false);
            await module.DisposeAsync().ConfigureAwait(false);
        }
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
    /// method should only be used when you only employ this database to store objects of a uniform
    /// type (or which inherit from a common type).
    /// </remarks>
    public async IAsyncEnumerable<TValue> GetAllAsync<TValue>()
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);

        if (jsonSerializerOptions is null)
        {
            var items = await module
                .InvokeAsync<TValue[]>("getAll", database)
                .ConfigureAwait(false);
            foreach (var item in items)
            {
                yield return item;
            }
            yield break;
        }

        var strings = await module
            .InvokeAsync<string[]>("getAllStrings", database)
            .ConfigureAwait(false);
        foreach (var item in strings)
        {
            var deserialized = JsonSerializer.Deserialize<IIdItem>(item, jsonSerializerOptions);
            if (deserialized is TValue value)
            {
                yield return value;
            }
        }
    }

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
    public async IAsyncEnumerable<TValue> GetBatchAsync<TValue>(bool reset = false)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);

        if (jsonSerializerOptions is null)
        {
            var items = await module
                .InvokeAsync<TValue[]>("getBatch", database, reset)
                .ConfigureAwait(false);
            foreach (var item in items)
            {
                yield return item;
            }
            yield break;
        }

        var strings = await module
            .InvokeAsync<string[]>("getBatchStrings", database, reset)
            .ConfigureAwait(false);
        foreach (var item in strings)
        {
            var deserialized = JsonSerializer.Deserialize<IIdItem>(item, jsonSerializerOptions);
            if (deserialized is TValue value)
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// Gets the <see cref="IIdItem"/> with the given <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to retrieve.</typeparam>
    /// <param name="id">The unique id of the item to retrieve.</param>
    /// <param name="cacheTimeout">
    /// Ignored. <see cref="IndexedDbService"/> does not cache results.
    /// </param>
    /// <returns>
    /// The item with the given id, or <see langword="null"/> if no item was found with that id.
    /// </returns>
    /// <remarks>
    /// This wraps <see cref="GetItemAsync{T}(string?, TimeSpan?)"/> in a <see cref="Task"/> and
    /// blocks on the result. Always use <see cref="GetItemAsync{T}(string?, TimeSpan?)"/> when
    /// possible.
    /// </remarks>
    public T? GetItem<T>(string? id, TimeSpan? cacheTimeout = null) where T : class, IIdItem
        => GetItemAsync<T>(id, cacheTimeout).AsTask().GetAwaiter().GetResult();

    /// <summary>
    /// Gets the <see cref="IIdItem"/> with the given <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to retrieve.</typeparam>
    /// <param name="id">The unique id of the item to retrieve.</param>
    /// <param name="cacheTimeout">
    /// Ignored. <see cref="IndexedDbService"/> does not cache results.
    /// </param>
    /// <returns>
    /// The item with the given id, or <see langword="null"/> if no item was found with that id.
    /// </returns>
    public async ValueTask<T?> GetItemAsync<T>(string? id, TimeSpan? cacheTimeout = null) where T : class, IIdItem
    {
        if (id is null)
        {
            return default;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);

        if (jsonSerializerOptions is null)
        {
            return await module
                .InvokeAsync<T>("getValue", database, id)
                .ConfigureAwait(false);
        }
        else
        {
            var item = await module
                .InvokeAsync<string>("getValueString", database, id)
                .ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(item, jsonSerializerOptions);
        }
    }

    /// <inheritdoc/>
    public IDataStoreQueryable<T> Query<T>() where T : class, IIdItem => new IndexedDbQueryable<T>(this);

    /// <summary>
    /// Removes the stored item with the given id.
    /// </summary>
    /// <typeparam name="T">The type of items to remove.</typeparam>
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
    /// This blocks on the result of <see cref="RemoveItemAsync{T}(string?)"/>. Always use <see
    /// cref="RemoveItemAsync{T}(string?)"/> when possible.
    /// </remarks>
    public bool RemoveItem<T>(string? id) where T : class, IIdItem
        => RemoveItemAsync<T>(id).GetAwaiter().GetResult();

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
    /// <see langword="true"/> if the item was successfully removed; otherwise <see
    /// langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This blocks on the result of <see cref="RemoveItemAsync{T}(T)"/>. Always use <see
    /// cref="RemoveItemAsync{T}(T)"/> when possible.
    /// </remarks>
    public bool RemoveItem<T>(T? item) where T : class, IIdItem
        => RemoveItem<T>(item?.Id);

    /// <inheritdoc/>
    public async Task<bool> RemoveItemAsync<T>(string? id) where T : class, IIdItem
    {
        if (id is null)
        {
            return true;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<bool>("deleteValue", database, id)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<bool> RemoveItemAsync<T>(T? item) where T : class, IIdItem
        => RemoveItemAsync<T>(item?.Id);

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to upsert.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <param name="cacheTimeout">
    /// Ignored. <see cref="IndexedDbService"/> does not cache results.
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
    /// This blocks on the result of <see cref="StoreItemAsync{T}(T, TimeSpan?)"/>. Always use <see
    /// cref="StoreItemAsync{T}(T, TimeSpan?)"/> when possible.
    /// </remarks>
    public bool StoreItem<T>(T? item, TimeSpan? cacheTimeout = null) where T : class, IIdItem
        => StoreItemAsync<T>(item, cacheTimeout).GetAwaiter().GetResult();

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to upsert.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <param name="cacheTimeout">
    /// Ignored. <see cref="IndexedDbService"/> does not cache results.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store;
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>,
    /// to indicate that the operation did not fail (even though no storage operation took
    /// place, neither did any failure).
    /// </remarks>
    public async Task<bool> StoreItemAsync<T>(T? item, TimeSpan? cacheTimeout = null) where T : class, IIdItem
    {
        if (item is null)
        {
            return true;
        }

        if (jsonSerializerOptions is null)
        {
            var module = await _moduleTask.Value.ConfigureAwait(false);

            // Explicitly serialize before invoking because T is passed as a plain object, and
            // implements IIdItem, which causes the default serialization behavior to serialize only
            // the interface properties.
            return await module
                .InvokeAsync<bool>("putValue", database, JsonSerializer.Serialize(item))
                .ConfigureAwait(false);
        }
        else
        {
            var module = await _moduleTask.Value.ConfigureAwait(false);
            return await module
                .InvokeAsync<bool>("putValue", database, JsonSerializer.Serialize<IIdItem>(item, jsonSerializerOptions))
                .ConfigureAwait(false);
        }
    }
}
