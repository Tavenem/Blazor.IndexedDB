using Microsoft.JSInterop;
using System.Text.Json;
using Tavenem.Blazor.IndexedDB;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extension for Tavenem.Blazor.ImageEditor.
/// </summary>
public static class MicrosoftExtensionsDependencyInjection
{
    /// <summary>
    /// Add support for Tavenem.Blazor.IndexedDB.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    public static void AddIndexedDbService(this IServiceCollection services)
        => services.AddScoped(provider => new IndexedDbService(provider.GetRequiredService<IJSRuntime>()));

    /// <summary>
    /// Add a Tavenem.Blazor.IndexedDB database to the services collection for use with dependency
    /// injection, using a <a
    /// href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection#keyed-services">keyed
    /// service name</a> which is the same as the <paramref name="databaseName"/> provided.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="databaseName">The name of the database.</param>
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
    /// Note that use of dependency injection for database instances is optional. They can also be
    /// initialized on demand with their public constructor, which requires an instance of <see
    /// cref="IndexedDbService"/>.
    /// </remarks>
    public static void AddIndexedDb(
        this IServiceCollection services,
        string databaseName,
        IEnumerable<string>? objectStores = null,
        int? version = null,
        JsonSerializerOptions? jsonSerializerOptions = null)
        => services.AddKeyedScoped(databaseName, (provider, name) => new IndexedDb(
            databaseName,
            provider.GetRequiredService<IndexedDbService>(),
            objectStores,
            version,
            jsonSerializerOptions));
}
