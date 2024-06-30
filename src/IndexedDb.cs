using System.Text.Json;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Information about an IndexedDB database.
/// </summary>
/// <param name="databaseName">The name of the database.</param>
/// <param name="indexedDbService">
/// An instance of <see cref="IndexedDbService"/> (typically provided by dependency injection).
/// </param>
/// <param name="version">The version number of the current schema.</param>
/// <param name="jsonSerializerOptions">
/// A configured <see cref="JsonSerializerOptions"/> instance. Optional.
/// </param>
/// <remarks>
/// <para>
/// See <a
/// href="https://developer.mozilla.org/en-US/docs/Web/API/IDBDatabase">https://developer.mozilla.org/en-US/docs/Web/API/IDBDatabase</a>
/// </para>
/// <para>
/// Note: this class supports direct indexing to access the <see cref="ObjectStores"/> dictionary
/// property. When getting a store by name on the <see cref="IndexedDb"/> object directly, if such a
/// store does not yet exist, it is created and returned.
/// </para>
/// </remarks>
public class IndexedDb(
    string databaseName,
    IndexedDbService indexedDbService,
    int? version = null,
    JsonSerializerOptions? jsonSerializerOptions = null)
{
    /// <summary>
    /// Provides direct access to the <see cref="ObjectStores"/> dictionary.
    /// </summary>
    /// <param name="name">The key to retrieve or set.</param>
    /// <returns>An <see cref="IndexedDbStore"/> instance.</returns>
    public IndexedDbStore this[string name]
    {
        get
        {
            if (ObjectStores.TryGetValue(name, out var store))
            {
                return store;
            }
            store = new(name, this);
            ObjectStores[name] = store;
            return store;
        }
        set => ObjectStores[name] = value;
    }

    /// <summary>
    /// The name of the database.
    /// </summary>
    public string DatabaseName { get; } = databaseName;

    /// <summary>
    /// A configured <see cref="JsonSerializerOptions"/> instance. Optional.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; } = jsonSerializerOptions;

    /// <summary>
    /// The collection of object stores associated with the current database.
    /// </summary>
    /// <remarks>
    /// Note: this dictionary can be accessed using index notation on the parent <see
    /// cref="IndexedDb"/> class object itself. When getting a store by name on the <see
    /// cref="IndexedDb"/> object directly, if such a store does not yet exist, it is created and
    /// returned.
    /// </remarks>
    public Dictionary<string, IndexedDbStore> ObjectStores { get; } = [];

    /// <summary>
    /// An instance of <see cref="IndexedDbService"/>.
    /// </summary>
    public IndexedDbService Service { get; } = indexedDbService;

    /// <summary>
    /// The version number of the current schema.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The use of this parameter is optional, but recommended (especially for
    /// writes).
    /// </para>
    /// <para>
    /// Update it to a higher value when you update the <see cref="DatabaseName"/>.
    /// This will automatically cause a new version of your database to be
    /// created with the new values, and avoid conflicts with the old schema.
    /// </para>
    /// <para>
    /// You can leave this value as <see langword="null"/> when retrieving data
    /// to automatically fetch from the latest version.
    /// </para>
    /// <para>
    /// Leaving it <see langword="null"/> when writing data to the database is
    /// also allowed, but may result in errors if the schema has changed.
    /// </para>
    /// </remarks>
    public int? Version { get; } = version;

    /// <summary>
    /// Adds a new object store.
    /// </summary>
    /// <param name="name">The name of the store to add.</param>
    /// <returns>
    /// The newly created object store.
    /// </returns>
    public IndexedDbStore AddStore(string name)
    {
        var store = new IndexedDbStore(name, this);
        ObjectStores.Add(name, store);
        return store;
    }

    /// <summary>
    /// Deletes the database.
    /// </summary>
    /// <remarks>
    /// Note: this may not take effect immediately, or at all, if there are open
    /// connections to the database.
    /// </remarks>
    public Task DeleteDatabaseAsync() => Service.DeleteDatabaseAsync(DatabaseName);
}
