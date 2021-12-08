using Microsoft.JSInterop;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Provides access to the IndexedDB API for a specific database.
/// </summary>
public class IndexedDbService<TKey> : IAsyncDisposable
{
    private const string DefaultKeyPath = "Id";

    private readonly IndexedDb<TKey> _database;
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="IndexedDbService"/>
    /// </summary>
    /// <param name="jsRuntime">An <see cref="IJSRuntime"/> instance.</param>
    /// <param name="database">An <see cref="IndexedDb{TKey}"/> instance.</param>
    public IndexedDbService(IJSRuntime jsRuntime, IndexedDb<TKey> database)
    {
        _database = database;
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Tavenem.Blazor.IndexedDB/tavenem-indexeddb.js").AsTask());
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
    /// Adds an item to an IndexedDB database.
    /// </summary>
    /// <typeparam name="TValue">The type of value being stored.</typeparam>
    /// <param name="value">The value to add.</param>
    /// <remarks>
    /// Adding fails if an item with the same key already exists. Use <see
    /// cref="PutValueAsync"/> instead to add <em>or</em> update a value.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="value"/> is missing the key property defined
    /// by the database, or it does not have the type
    /// <typeparamref name="TKey"/>, or the value of the key is <see
    /// langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if an item with the same key already exists.
    /// </exception>
    public async Task AddValueAsync<TValue>(TValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var keyPath = _database.KeyPath ?? DefaultKeyPath;
        var keyProperty = value.GetType().GetProperty(keyPath);
        if (keyProperty is null)
        {
            throw new ArgumentException($"{nameof(value)} is missing {keyPath} property.", nameof(value));
        }
        if (keyProperty.PropertyType != typeof(TKey))
        {
            throw new ArgumentException($"The type of the {keyPath} property for {nameof(value)} is not {typeof(TKey).Name}.", nameof(value));
        }
        var key = keyProperty.GetValue(value);
        if (key is null)
        {
            throw new ArgumentException($"The {keyPath} property for {nameof(value)} is null.", nameof(value));
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("addValue", _database, value)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Clears an IndexedDB object store.
    /// </summary>
    public async Task ClearAsync()
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("clear", _database)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the number of items in an IndexedDB object store.
    /// </summary>
    public async Task<long> CountAsync()
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<long>("count", _database)
            .ConfigureAwait(false);
    }

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
            .InvokeVoidAsync("deleteDatabase", _database.DatabaseName)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the item with the given key from an IndexedDB database.
    /// </summary>
    /// <param name="key">The key of the item to delete.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    public async Task DeleteKeyAsync(TKey? key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("deleteValue", _database, key)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an item from an IndexedDB database.
    /// </summary>
    /// <typeparam name="TValue">The type of value being deleted.</typeparam>
    /// <param name="value">The value to delete.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="value"/> is missing the key property defined
    /// by the database, or it does not have the type
    /// <typeparamref name="TKey"/>, or the key property's value is <see
    /// langword="null"/>.
    /// </exception>
    public async Task DeleteValueAsync<TValue>(TValue value)
    {
        if (value is null)
        {
            return;
        }

        var keyPath = _database.KeyPath ?? DefaultKeyPath;
        var keyProperty = value.GetType().GetProperty(keyPath);
        if (keyProperty is null)
        {
            throw new ArgumentException($"{nameof(value)} is missing {keyPath} property.", nameof(value));
        }
        if (keyProperty.PropertyType != typeof(TKey))
        {
            throw new ArgumentException($"The type of the {keyPath} property for {nameof(value)} is not {typeof(TKey).Name}.", nameof(value));
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);

        var key = keyProperty.GetValue(value);
        if (key is null)
        {
            throw new ArgumentException($"The value of the {keyPath} property for {nameof(value)} is null.", nameof(value));
        }

        var typedKey = (TKey)key;
        await module
            .InvokeVoidAsync("deleteValue", _database, typedKey)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the items in an IndexedDB object store.
    /// </summary>
    /// <typeparam name="TValue">The type of value being retrieved.</typeparam>
    public async Task<TValue[]> GetAllAsync<TValue>()
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<TValue[]>("getAll", _database)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the item with the given key from an IndexedDB database.
    /// </summary>
    /// <typeparam name="TValue">The type of value being retrieved.</typeparam>
    /// <param name="key">The key of the item to retrieve.</param>
    /// <returns>
    /// The item with the given key, or <see langword="null"/> if no item is
    /// found with that key.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    public async Task<TValue> GetValueAsync<TValue>(TKey? key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<TValue>("getValue", _database, key)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Stores an item in an IndexedDB database, or updates the item with the
    /// same key if it already exists.
    /// </summary>
    /// <typeparam name="TValue">The type of value being stored.</typeparam>
    /// <param name="value">The value to add.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="value"/> is missing the key property defined
    /// by the database, or it does not have the type
    /// <typeparamref name="TKey"/>, or the value of the key is <see
    /// langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the put operation fails.
    /// </exception>
    public async Task PutValueAsync<TValue>(TValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var keyPath = _database.KeyPath ?? DefaultKeyPath;
        var keyProperty = value.GetType().GetProperty(keyPath);
        if (keyProperty is null)
        {
            throw new ArgumentException($"{nameof(value)} is missing {keyPath} property.", nameof(value));
        }
        if (keyProperty.PropertyType != typeof(TKey))
        {
            throw new ArgumentException($"The type of the {keyPath} property for {nameof(value)} is not {typeof(TKey).Name}.", nameof(value));
        }
        var key = keyProperty.GetValue(value);
        if (key is null)
        {
            throw new ArgumentException($"The {keyPath} property for {nameof(value)} is null.", nameof(value));
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("putValue", _database, value)
            .ConfigureAwait(false);
    }
}
