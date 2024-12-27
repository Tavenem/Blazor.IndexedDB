using System.Text.Json;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Information about an IndexedDB database.
/// </summary>
/// <param name="databaseName">The name of the database.</param>
/// <param name="indexedDbService">
/// An instance of <see cref="IndexedDbService"/> (typically provided by dependency injection).
/// </param>
/// <param name="objectStores">
/// <para>
/// The names of all object stores associated with the current database schema version.
/// </para>
/// <para>
/// The name of the database itself will be used as the name of a single object store if no
/// store names are provided.
/// </para>
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
/// Note: object stores cannot be added dynamically to an existing database. To add a new object
/// store, re-create the database with the new store included, and assign it a greater version
/// number than the previous registered version.
/// </para>
/// </remarks>
public class IndexedDb(
    string databaseName,
    IndexedDbService indexedDbService,
    IEnumerable<string>? objectStores = null,
    int? version = null,
    JsonSerializerOptions? jsonSerializerOptions = null)
{
    /// <summary>
    /// Retrieve an <see cref="IndexedDbStore"/> by its <see cref="IndexedDbStore.StoreName"/>.
    /// </summary>
    /// <param name="name">The <see cref="IndexedDbStore.StoreName"/> to retrieve.</param>
    /// <returns>
    /// An <see cref="IndexedDbStore"/> instance; or <see langword="null"/> if there is no store
    /// registered by that name.
    /// </returns>
    public IndexedDbStore? this[string name]
    {
        get
        {
            if (ObjectStores.TryGetValue(name, out var store))
            {
                if (store is null)
                {
                    store = new(name, this);
                    ObjectStores[name] = store;
                }
                return store;
            }
            return null;
        }
    }

    /// <summary>
    /// The name of the database.
    /// </summary>
    public string DatabaseName { get; } = databaseName;

    /// <summary>
    /// The names of all object stores associated with the current database schema version.
    /// </summary>
    public virtual IEnumerable<string> ObjectStoreNames => ObjectStores.Keys;

    /// <summary>
    /// A configured <see cref="JsonSerializerOptions"/> instance. Optional.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; } = jsonSerializerOptions;

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
    /// The collection of object stores associated with the current database.
    /// </summary>
    /// <remarks>
    /// Note: this dictionary can be accessed using index notation on the parent <see
    /// cref="IndexedDb"/> class object itself. When getting a store by name on the <see
    /// cref="IndexedDb"/> object directly, if such a store does not yet exist, it is created and
    /// returned.
    /// </remarks>
    internal Dictionary<string, IndexedDbStore?> ObjectStores { get; }
        = objectStores?.ToDictionary(x => x, x => (IndexedDbStore?)null)
        ?? new Dictionary<string, IndexedDbStore?>([new KeyValuePair<string, IndexedDbStore?>(databaseName, null)]);

    /// <summary>
    /// Deletes the database.
    /// </summary>
    /// <remarks>
    /// Note: this may not take effect immediately, or at all, if there are open
    /// connections to the database.
    /// </remarks>
    public Task DeleteDatabaseAsync() => Service.DeleteDatabaseAsync(DatabaseName);
}
