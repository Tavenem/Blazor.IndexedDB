using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Provides access to the IndexedDB API for a specific database.
/// </summary>
/// <param name="jsRuntime">An <see cref="IJSRuntime"/> instance.</param>
public class IndexedDbService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask
        = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Tavenem.Blazor.IndexedDB/tavenem-indexeddb.js").AsTask());

    private bool _disposed;

    /// <summary>
    /// Clears an object store.
    /// </summary>
    /// <param name="store">
    /// The <see cref="IndexedDbStore"/>.
    /// </param>
    public async Task ClearAsync(IndexedDbStore store)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("clear", store.Info)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the number of items in the object store.
    /// </summary>
    /// <param name="store">
    /// The <see cref="IndexedDbStore"/>.
    /// </param>
    public async Task<long> CountAsync(IndexedDbStore store)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<long>("count", store.Info)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the database.
    /// </summary>
    /// <param name="name">The name of the database.</param>
    /// <remarks>
    /// Note: this may not take effect immediately, or at all, if there are open
    /// connections to the database.
    /// </remarks>
    public async Task DeleteDatabaseAsync(string name)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("deleteDatabase", name)
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
    /// <param name="store">The <see cref="IndexedDbStore"/>.</param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="TValue"/>.
    /// </param>
    /// <remarks>
    /// Note: the IndexedDB object store cannot filter items by type. Using this method when there
    /// are objects of different types in your data store will result in an exception when
    /// attempting to deserialize items of types other than <typeparamref name="TValue"/>. This
    /// method should only be used when you only employ this object store to store objects of a uniform
    /// type (or which inherit from a common type).
    /// </remarks>
    public async IAsyncEnumerable<TValue> GetAllAsync<TValue>(IndexedDbStore store, JsonTypeInfo<TValue>? typeInfo = null)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);

        if (typeInfo is null && store.Database.JsonSerializerOptions is null)
        {
            var items = await module
                .InvokeAsync<TValue[]>("getAll", store.Info)
                .ConfigureAwait(false);
            foreach (var item in items)
            {
                yield return item;
            }
            yield break;
        }

        var strings = await module
            .InvokeAsync<string[]>("getAllStrings", store.Info)
            .ConfigureAwait(false);

        foreach (var item in strings)
        {
            if (string.IsNullOrEmpty(item))
            {
                continue;
            }

            TValue? value;
            try
            {
                value = typeInfo is null
                    ? JsonSerializer.Deserialize<TValue>(item, store.Database.JsonSerializerOptions)
                    : JsonSerializer.Deserialize(item, typeInfo);
            }
            catch
            {
                continue;
            }
            if (value is not null)
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// Retrieves a batch of items from an IndexedDB object store.
    /// </summary>
    /// <typeparam name="TValue">The type of value being retrieved.</typeparam>
    /// <param name="store">
    /// The <see cref="IndexedDbStore"/>.
    /// </param>
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
    /// cref="IndexedDbStore.Query{T}"/> and one of the <see cref="IDataStoreQueryable{T}"/>
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
    public async IAsyncEnumerable<TValue> GetBatchAsync<TValue>(IndexedDbStore store, bool reset = false, JsonTypeInfo<TValue>? typeInfo = null)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);

        if (typeInfo is null && store.Database.JsonSerializerOptions is null)
        {
            var items = await module
                .InvokeAsync<TValue[]>("getBatch", store.Info, reset)
                .ConfigureAwait(false);
            foreach (var item in items)
            {
                yield return item;
            }
            yield break;
        }

        var strings = await module
            .InvokeAsync<string[]>("getBatchStrings", store.Info, reset)
            .ConfigureAwait(false);

        foreach (var item in strings)
        {
            if (string.IsNullOrEmpty(item))
            {
                continue;
            }

            TValue? value;
            try
            {
                value = typeInfo is null
                    ? JsonSerializer.Deserialize<TValue>(item, store.Database.JsonSerializerOptions)
                    : JsonSerializer.Deserialize(item, typeInfo);
            }
            catch
            {
                continue;
            }
            if (value is not null)
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// Gets the object with the given <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of object to retrieve.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore"/>.</param>
    /// <param name="id">The unique id of the item to retrieve.</param>
    /// <returns>
    /// The item with the given id, or <see langword="null"/> if no item was found with that id.
    /// </returns>
    /// <remarks>
    /// Note: this overload will typically fail in the browser (or whenever trimming is enabled),
    /// since it relies on reflection-based (de)serialization. To use source generated
    /// deserialization, use the overload which takes a <see cref="JsonTypeInfo{T}"/>.
    /// </remarks>
    public async ValueTask<TValue?> GetItemAsync<TValue>(IndexedDbStore store, string? id)
    {
        if (id is null)
        {
            return default;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);

        if (store.Database.JsonSerializerOptions is null)
        {
            return await module
                .InvokeAsync<TValue>("getValue", store.Info, id)
                .ConfigureAwait(false);
        }
        else
        {
            var item = await module
                .InvokeAsync<string>("getValueString", store.Info, id)
                .ConfigureAwait(false);
            return string.IsNullOrEmpty(item)
                ? default
                : JsonSerializer.Deserialize<TValue>(item, store.Database.JsonSerializerOptions);
        }
    }

    /// <summary>
    /// Gets the object with the given <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of object to retrieve.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore"/>.</param>
    /// <param name="id">The unique id of the item to retrieve.</param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="TValue"/>.
    /// </param>
    /// <returns>
    /// The item with the given id, or <see langword="null"/> if no item was found with that id.
    /// </returns>
    public async ValueTask<TValue?> GetItemAsync<TValue>(IndexedDbStore store, string? id, JsonTypeInfo<TValue>? typeInfo)
    {
        if (id is null)
        {
            return default;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);

        if (typeInfo is null && store.Database.JsonSerializerOptions is null)
        {
            return await module
                .InvokeAsync<TValue>("getValue", store.Info, id)
                .ConfigureAwait(false);
        }
        else
        {
            var item = await module
                .InvokeAsync<string>("getValueString", store.Info, id)
                .ConfigureAwait(false);
            if (string.IsNullOrEmpty(item))
            {
                return default;
            }

            return typeInfo is null
                ? JsonSerializer.Deserialize<TValue>(item, store.Database.JsonSerializerOptions)
                : JsonSerializer.Deserialize(item, typeInfo);
        }
    }

    /// <summary>
    /// Removes the item with the given id.
    /// </summary>
    /// <param name="store">The <see cref="IndexedDbStore"/>.</param>
    /// <param name="id">The id of the item to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the removal succeeded, or there was no such item; otherwise <see
    /// langword="false"/>.
    /// </returns>
    public async Task<bool> RemoveItemAsync(IndexedDbStore store, string? id)
    {
        if (id is null)
        {
            return true;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<bool>("deleteValue", store.Info, id)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of object to upsert.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore"/>.</param>
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
    /// Note: this differs from <see cref="StoreItemAsync{T}(IndexedDbStore, T)"/> in that <see
    /// cref="StoreItemAsync{T}(IndexedDbStore, T)"/> expects that <paramref name="item"/>
    /// implements <see cref="IIdItem"/>, and serializes it accordingly.
    /// </para>
    /// </remarks>
    public async Task<bool> StoreAsync<T>(IndexedDbStore store, T? item) where T : class
    {
        if (item is null)
        {
            return true;
        }

        if (store.Database.JsonSerializerOptions is null)
        {
            var module = await _moduleTask.Value.ConfigureAwait(false);

            // Explicitly serialize before invoking because T is passed as a plain object, and
            // implements IIdItem, which causes the default serialization behavior to serialize only
            // the interface properties.
            return await module
                .InvokeAsync<bool>("putValue", store.Info, JsonSerializer.Serialize(item))
                .ConfigureAwait(false);
        }
        else
        {
            var module = await _moduleTask.Value.ConfigureAwait(false);
            return await module
                .InvokeAsync<bool>("putValue", store.Info, JsonSerializer.Serialize(item, store.Database.JsonSerializerOptions))
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to upsert.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore"/>.</param>
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
    /// Note: this differs from <see cref="StoreItemAsync{T}(IndexedDbStore, T, JsonTypeInfo{T})"/>
    /// in that <see cref="StoreItemAsync{T}(IndexedDbStore, T, JsonTypeInfo{T})"/> expects that
    /// <paramref name="item"/> implements <see cref="IIdItem"/>, and serializes it accordingly.
    /// </para>
    /// </remarks>
    public async Task<bool> StoreAsync<T>(IndexedDbStore store, T? item, JsonTypeInfo<T>? typeInfo) where T : class
    {
        if (item is null)
        {
            return true;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);

        // In each case, the value is explicitly serialized before invoking to ensure that the
        // correct options are used, rather than whatever happens to be configured for JavaScript
        // interop.

        if (store.Database.JsonSerializerOptions is not null)
        {
            return await module
                .InvokeAsync<bool>("putValue", store.Info, JsonSerializer.Serialize(item, store.Database.JsonSerializerOptions))
                .ConfigureAwait(false);
        }

        if (typeInfo is not null)
        {
            return await module
                .InvokeAsync<bool>("putValue", store.Info, JsonSerializer.Serialize(item, typeInfo))
                .ConfigureAwait(false);
        }

        return await module
            .InvokeAsync<bool>("putValue", store.Info, JsonSerializer.Serialize(item))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to upsert.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore"/>.</param>
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
    /// Note: this differs from <see cref="StoreAsync{T}(IndexedDbStore, T)"/> in that this method
    /// expects that <paramref name="item"/> implements <see cref="IIdItem"/>, and serializes it
    /// accordingly, whereas <see cref="StoreAsync{T}(IndexedDbStore, T)"/> does not.
    /// </para>
    /// </remarks>
    public async Task<bool> StoreItemAsync<T>(IndexedDbStore store, T? item) where T : class, IIdItem
    {
        if (item is null)
        {
            return true;
        }

        if (store.Database.JsonSerializerOptions is null)
        {
            var module = await _moduleTask.Value.ConfigureAwait(false);

            // Explicitly serialize before invoking because T is passed as a plain object, and
            // implements IIdItem, which causes the default serialization behavior to serialize only
            // the interface properties.
            return await module
                .InvokeAsync<bool>("putValue", store.Info, JsonSerializer.Serialize(item))
                .ConfigureAwait(false);
        }
        else
        {
            var module = await _moduleTask.Value.ConfigureAwait(false);
            return await module
                .InvokeAsync<bool>("putValue", store.Info, JsonSerializer.Serialize<IIdItem>(item, store.Database.JsonSerializerOptions))
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IIdItem"/> to upsert.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore"/>.</param>
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
    /// Note: this differs from <see cref="StoreAsync{T}(IndexedDbStore, T, JsonTypeInfo{T})"/> in
    /// that this method expects that <paramref name="item"/> implements <see cref="IIdItem"/>, and
    /// serializes it accordingly, whereas <see cref="StoreAsync{T}(IndexedDbStore, T,
    /// JsonTypeInfo{T})"/> does not.
    /// </para>
    /// </remarks>
    public async Task<bool> StoreItemAsync<T>(IndexedDbStore store, T? item, JsonTypeInfo<T>? typeInfo) where T : class, IIdItem
    {
        if (item is null)
        {
            return true;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);

        // In each case, the value is explicitly serialized before invoking to ensure that the
        // correct options are used, rather than whatever happens to be configured for JavaScript
        // interop.

        if (store.Database.JsonSerializerOptions is not null)
        {
            return await module
                .InvokeAsync<bool>("putValue", store.Info, JsonSerializer.Serialize<IIdItem>(item, store.Database.JsonSerializerOptions))
                .ConfigureAwait(false);
        }

        if (typeInfo is not null)
        {
            return await module
                .InvokeAsync<bool>("putValue", store.Info, JsonSerializer.Serialize(item, typeInfo))
                .ConfigureAwait(false);
        }

        return await module
            .InvokeAsync<bool>("putValue", store.Info, JsonSerializer.Serialize(item))
            .ConfigureAwait(false);
    }
}
