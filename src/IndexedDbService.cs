using Microsoft.JSInterop;
using System.Runtime.CompilerServices;
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
    /// <typeparam name="TItem">
    /// A shared interface for all stored items in the <paramref name="store"/>.
    /// </typeparam>
    /// <param name="store">The <see cref="IndexedDbStore{TItem}"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    public async Task ClearAsync<TItem>(
        IndexedDbStore<TItem> store,
        CancellationToken cancellationToken = default) where TItem : notnull
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("clear", cancellationToken, store.Info)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the number of items in the object store.
    /// </summary>
    /// <typeparam name="TItem">
    /// A shared interface for all stored items in the <paramref name="store"/>.
    /// </typeparam>
    /// <param name="store">The <see cref="IndexedDbStore{TItem}"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The number of items in the object store.</returns>
    public async Task<long> CountAsync<TItem>(
        IndexedDbStore<TItem> store,
        CancellationToken cancellationToken = default) where TItem : notnull
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<long>("count", cancellationToken, store.Info)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the database.
    /// </summary>
    /// <param name="name">The name of the database.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <remarks>
    /// Note: this may not take effect immediately, or at all, if there are open
    /// connections to the database.
    /// </remarks>
    public async Task DeleteDatabaseAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("deleteDatabase", cancellationToken, name)
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
    /// <typeparam name="TItem">
    /// A shared interface for all stored items in the <paramref name="store"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The type of object to retrieve.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore{TItem}"/>.</param>
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
    public async IAsyncEnumerable<TValue> GetAllAsync<TItem, TValue>(
        IndexedDbStore<TItem> store,
        JsonTypeInfo<TValue>? typeInfo = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TItem : notnull
        where TValue : TItem
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);

        if (typeInfo is null && store.Database.JsonSerializerOptions is null)
        {
            var items = await module
                .InvokeAsync<TValue[]>("getAll", cancellationToken, store.Info)
                .ConfigureAwait(false);
            foreach (var item in items)
            {
                yield return item;
            }
            yield break;
        }

        var strings = await module
            .InvokeAsync<string[]>("getAll", cancellationToken, store.Info, true)
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

            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Iterates all items from an IndexedDB object store.
    /// </summary>
    /// <typeparam name="TItem">
    /// A shared interface for all stored items in the <paramref name="store"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The type of object to retrieve.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore{TItem}"/>.</param>
    /// <param name="skip">
    /// The number of items to skip. Optional.
    /// </param>
    /// <param name="take">
    /// The maximum number of items to take. Optional.
    /// </param>
    /// <param name="typeDiscriminator">
    /// The name of a type discriminator property. Optional.
    /// </param>
    /// <param name="typeDiscriminatorValue">
    /// The value of the type discriminator property which items must have in order to be returned. Optional.
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
    public async IAsyncEnumerable<TValue> GetAllBatchesAsync<TItem, TValue>(
        IndexedDbStore<TItem> store,
        int? skip = null,
        int? take = null,
        string? typeDiscriminator = null,
        string? typeDiscriminatorValue = null,
        JsonTypeInfo<TValue>? typeInfo = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TItem : notnull
        where TValue : TItem
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);

        var options = new BatchOptions(
            skip,
            take,
            typeDiscriminator,
            typeDiscriminatorValue);

        if (typeInfo is null && store.Database.JsonSerializerOptions is null)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var (items, continuationKey) = await module
                    .InvokeAsync<BatchResult<TValue>>("getBatch", cancellationToken, store.Info, options)
                    .ConfigureAwait(false);
                foreach (var item in items)
                {
                    yield return item;
                }
                if (string.IsNullOrEmpty(continuationKey))
                {
                    yield break;
                }
                options = options with
                {
                    Skip = null,
                    Take = options.Take is null ? null : options.Take - items.Count,
                    ContinuationKey = continuationKey,
                };
            }
            yield break;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var (strings, continuationKey) = await module
                .InvokeAsync<BatchResult<string>>("getBatch", cancellationToken, store.Info, options, true)
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

                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
            }

            if (string.IsNullOrEmpty(continuationKey))
            {
                yield break;
            }

            options = options with
            {
                Skip = null,
                Take = options.Take is null ? null : options.Take - strings.Count,
                ContinuationKey = continuationKey,
            };
        }
    }

    /// <summary>
    /// Retrieves a batch of items from an IndexedDB object store.
    /// </summary>
    /// <typeparam name="TItem">
    /// A shared interface for all stored items in the <paramref name="store"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The type of object to retrieve.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore{TItem}"/>.</param>
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
    /// <param name="typeDiscriminator">
    /// The name of a type discriminator property. Optional.
    /// </param>
    /// <param name="typeDiscriminatorValue">
    /// The value of the type discriminator property which items must have in order to be returned.
    /// Optional.
    /// </param>
    /// <param name="continuationKey">
    /// A continuation key (from the return value of a previous call to this method).
    /// </param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="TValue"/>.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// A list of results for this batch, and a continuation key for the next batch, if there are
    /// more results.
    /// </returns>
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
    public async Task<(List<TValue> Items, string? ContinuationKey)> GetBatchAsync<TItem, TValue>(
        IndexedDbStore<TItem> store,
        int? skip = null,
        int? take = null,
        string? typeDiscriminator = null,
        string? typeDiscriminatorValue = null,
        string? continuationKey = null,
        JsonTypeInfo<TValue>? typeInfo = null,
        CancellationToken cancellationToken = default)
        where TItem : notnull
        where TValue : TItem
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);

        var options = new BatchOptions(
            skip,
            take,
            typeDiscriminator,
            typeDiscriminatorValue,
            continuationKey);

        if (typeInfo is null && store.Database.JsonSerializerOptions is null)
        {
            var (items, nextContinuationKey) = await module
                .InvokeAsync<BatchResult<TValue>>("getBatch", cancellationToken, store.Info, options)
                .ConfigureAwait(false);
            return (items, nextContinuationKey);
        }

        var (strings, nextStringContinuationKey) = await module
            .InvokeAsync<BatchResult<string>>("getBatch", cancellationToken, store.Info, options, true)
            .ConfigureAwait(false);

        var values = new List<TValue>(strings.Count);
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
                values.Add(value);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
        return (values, nextStringContinuationKey);
    }

    /// <summary>
    /// Gets the object with the given <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TItem">
    /// A shared interface for all stored items in the <paramref name="store"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The type of object to retrieve.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore{TItem}"/>.</param>
    /// <param name="id">The unique id of the item to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// The item with the given id, or <see langword="null"/> if no item was found with that id.
    /// </returns>
    /// <remarks>
    /// Note: this overload will typically fail in the browser (or whenever trimming is enabled),
    /// since it relies on reflection-based (de)serialization. To use source generated
    /// deserialization, use the overload which takes a <see cref="JsonTypeInfo{T}"/>.
    /// </remarks>
    public async ValueTask<TValue?> GetItemAsync<TItem, TValue>(
        IndexedDbStore<TItem> store,
        string? id,
        CancellationToken cancellationToken = default)
        where TItem : notnull
        where TValue : TItem
    {
        if (id is null)
        {
            return default;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);

        if (store.Database.JsonSerializerOptions is null)
        {
            return await module
                .InvokeAsync<TValue>("getValue", cancellationToken, store.Info, id)
                .ConfigureAwait(false);
        }
        else
        {
            var item = await module
                .InvokeAsync<string>("getValue", cancellationToken, store.Info, id, true)
                .ConfigureAwait(false);
            return string.IsNullOrEmpty(item)
                ? default
                : JsonSerializer.Deserialize<TValue>(item, store.Database.JsonSerializerOptions);
        }
    }

    /// <summary>
    /// Gets the object with the given <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TItem">
    /// A shared interface for all stored items in the <paramref name="store"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The type of object to retrieve.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore{TItem}"/>.</param>
    /// <param name="id">The unique id of the item to retrieve.</param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="TValue"/>.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// The item with the given id, or <see langword="null"/> if no item was found with that id.
    /// </returns>
    public async ValueTask<TValue?> GetItemAsync<TItem, TValue>(
        IndexedDbStore<TItem> store,
        string? id,
        JsonTypeInfo<TValue>? typeInfo,
        CancellationToken cancellationToken = default)
        where TItem : notnull
        where TValue : TItem
    {
        if (id is null)
        {
            return default;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);

        if (typeInfo is null && store.Database.JsonSerializerOptions is null)
        {
            return await module
                .InvokeAsync<TValue>("getValue", cancellationToken, store.Info, id)
                .ConfigureAwait(false);
        }
        else
        {
            var item = await module
                .InvokeAsync<string>("getValue", cancellationToken, store.Info, id, true)
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
    /// <typeparam name="TItem">
    /// A shared interface for all stored items in the <paramref name="store"/>.
    /// </typeparam>
    /// <param name="store">The <see cref="IndexedDbStore{TItem}"/>.</param>
    /// <param name="id">The id of the item to remove.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the removal succeeded, or there was no such item; otherwise <see
    /// langword="false"/>.
    /// </returns>
    public async ValueTask<bool> RemoveItemAsync<TItem>(
        IndexedDbStore<TItem> store,
        string? id,
        CancellationToken cancellationToken = default) where TItem : notnull
    {
        if (id is null)
        {
            return true;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<bool>("deleteValue", cancellationToken, store.Info, id)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="TItem">
    /// A shared interface for all stored items in the <paramref name="store"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The type of object to upsert.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore{TItem}"/>.</param>
    /// <param name="item">The item to store.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>, to
    /// indicate that the operation did not fail (even though no storage operation took place,
    /// neither did any failure).
    /// </remarks>
    public async ValueTask<TValue?> StoreAsync<TItem, TValue>(
        IndexedDbStore<TItem> store,
        TValue? item,
        CancellationToken cancellationToken = default)
        where TItem : notnull
        where TValue : TItem
    {
        if (item is null)
        {
            return item;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);

        // In each case, the value is explicitly serialized before invoking to ensure that the
        // correct options are used, rather than whatever happens to be configured for JavaScript
        // interop.

        bool success;
        if (store.Database.JsonSerializerOptions is null)
        {
            success = await module
                .InvokeAsync<bool>("putValue", cancellationToken, store.Info, JsonSerializer.Serialize(item))
                .ConfigureAwait(false);
        }
        else if (item is IIdItem idItem)
        {
            success = await module
                .InvokeAsync<bool>("putValue", cancellationToken, store.Info, JsonSerializer.Serialize(idItem, store.Database.JsonSerializerOptions))
                .ConfigureAwait(false);
        }
        else
        {
            success = await module
                .InvokeAsync<bool>("putValue", cancellationToken, store.Info, JsonSerializer.Serialize(item, store.Database.JsonSerializerOptions))
                .ConfigureAwait(false);
        }
        return success
            ? item
            : default;
    }

    /// <summary>
    /// Upserts the given <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="TItem">
    /// A shared interface for all stored items in the <paramref name="store"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The type of object to upsert.</typeparam>
    /// <param name="store">The <see cref="IndexedDbStore{TItem}"/>.</param>
    /// <param name="item">The item to store.</param>
    /// <param name="typeInfo">
    /// <see cref="JsonTypeInfo{T}"/> for <typeparamref name="TValue"/>.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully persisted to the data store; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If the item is <see langword="null"/>, does nothing and returns <see langword="true"/>, to
    /// indicate that the operation did not fail (even though no storage operation took place,
    /// neither did any failure).
    /// </remarks>
    public async ValueTask<TValue?> StoreAsync<TItem, TValue>(
        IndexedDbStore<TItem> store,
        TValue? item,
        JsonTypeInfo<TValue>? typeInfo,
        CancellationToken cancellationToken = default)
        where TItem : notnull
        where TValue : TItem
    {
        if (item is null)
        {
            return item;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);

        // In each case, the value is explicitly serialized before invoking to ensure that the
        // correct options are used, rather than whatever happens to be configured for JavaScript
        // interop.

        bool success;
        if (store.Database.JsonSerializerOptions is not null)
        {
            success = item is IIdItem idItem
                ? await module
                    .InvokeAsync<bool>(
                        "putValue",
                        cancellationToken,
                        store.Info,
                        JsonSerializer.Serialize<IIdItem>(idItem, store.Database.JsonSerializerOptions))
                    .ConfigureAwait(false)
                : await module
                    .InvokeAsync<bool>(
                        "putValue",
                        cancellationToken,
                        store.Info,
                        JsonSerializer.Serialize(item, store.Database.JsonSerializerOptions))
                    .ConfigureAwait(false);
        }
        else if (typeInfo is not null)
        {
            success = await module
                .InvokeAsync<bool>("putValue", cancellationToken, store.Info, JsonSerializer.Serialize(item, typeInfo))
                .ConfigureAwait(false);
        }
        else
        {
            success = await module
                .InvokeAsync<bool>("putValue", cancellationToken, store.Info, JsonSerializer.Serialize(item))
                .ConfigureAwait(false);
        }
        return success
            ? item
            : default;
    }
}
