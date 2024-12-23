﻿using Microsoft.JSInterop;
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
    /// <param name="initialVersion">The version number of the current schema.Each time a new objectStore is added, the current version is incremented by 1</param>
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
        int? initialVersion = 1,
        JsonSerializerOptions? jsonSerializerOptions = null)
        => services.AddKeyedScoped(databaseName, (provider, name) => new IndexedDb(
            databaseName,
            provider.GetRequiredService<IndexedDbService>(),
            initialVersion,
            jsonSerializerOptions));
}
