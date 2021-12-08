using Microsoft.JSInterop;
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
    public static void AddIndexedDb(this IServiceCollection services)
        => services.AddScoped<IndexedDbService>();

    /// <summary>
    /// Add support for Tavenem.Blazor.IndexedDB with a specific database.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="database">An <see cref="IndexedDb{TKey}"/> instance.</param>
    public static void AddIndexedDb<TKey>(this IServiceCollection services, IndexedDb<TKey> database)
        => services.AddScoped(provider => new IndexedDbService<TKey>(
            provider.GetRequiredService<IJSRuntime>(),
            database));

    /// <summary>
    /// Add support for Tavenem.Blazor.IndexedDB with a specific database.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="databaseImplementationFactory">The factory that creates an <see cref="IndexedDb{TKey}"/> instance.</param>
    public static void AddIndexedDb<TKey>(
        this IServiceCollection services,
        Func<IServiceProvider, IndexedDb<TKey>> databaseImplementationFactory)
        => services.AddScoped(provider => new IndexedDbService<TKey>(
            provider.GetRequiredService<IJSRuntime>(),
            databaseImplementationFactory(provider)));
}
