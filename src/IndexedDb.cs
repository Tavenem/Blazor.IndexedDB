namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Information about an IndexedDB database.
/// </summary>
/// <param name="databaseName">The name of the database.</param>
/// <param name="version">The version number of the current schema.</param>
/// <param name="storeName">
/// <para>
/// The name of the object store.
/// </para>
/// <para>
/// If left <see langword="null"/> or empty, the <see cref="DatabaseName"/>
/// will be reused.
/// </para>
/// </param>
public class IndexedDb(string databaseName, int? version = null, string? storeName = null)
{
    /// <summary>
    /// The name of the database.
    /// </summary>
    public string DatabaseName { get; set; } = databaseName;

    /// <summary>
    /// <para>
    /// The name of the object store.
    /// </para>
    /// <para>
    /// If left <see langword="null"/> or empty, the <see cref="DatabaseName"/>
    /// will be reused.
    /// </para>
    /// </summary>
    public string? StoreName { get; set; } = storeName;

    /// <summary>
    /// The version number of the current schema.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The use of this parameter is optional, but recommended (especially for
    /// writes).
    /// </para>
    /// <para>
    /// Update it to a higher value when you update the <see cref="StoreName"/>.
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
    public int? Version { get; set; } = version;
}
