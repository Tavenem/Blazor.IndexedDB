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
    /// <param name="database">An <see cref="IndexedDb"/> instance.</param>
    /// <param name="jsonSerializerOptions">A configured <see cref="JsonSerializerOptions"/> instance. Optional.</param>
    public static void AddIndexedDb(
        this IServiceCollection services,
        IndexedDb database,
        JsonSerializerOptions? jsonSerializerOptions = null)
        => services.AddScoped(provider => new IndexedDbService(
            provider.GetRequiredService<IJSRuntime>(),
            database,
            jsonSerializerOptions));

    /// <summary>
    /// Add support for Tavenem.Blazor.IndexedDB.
    /// </summary>
    /// <param name="services">An <see cref="IServiceCollection"/> instance.</param>
    /// <param name="databaseImplementationFactory">The factory that creates an <see cref="IndexedDb"/> instance.</param>
    /// <param name="options">A configured <see cref="JsonSerializerOptions"/> instance. Optional.</param>
    public static void AddIndexedDb(
        this IServiceCollection services,
        Func<IServiceProvider, IndexedDb> databaseImplementationFactory,
        JsonSerializerOptions? options = null)
        => services.AddScoped(provider => new IndexedDbService(
            provider.GetRequiredService<IJSRuntime>(),
            databaseImplementationFactory(provider),
            options));
}
