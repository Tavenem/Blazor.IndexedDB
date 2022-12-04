![build](https://img.shields.io/github/workflow/status/Tavenem/Blazor.IndexedDB/publish/main) [![NuGet downloads](https://img.shields.io/nuget/dt/Tavenem.Blazor.IndexedDB)](https://www.nuget.org/packages/Tavenem.Blazor.IndexedDB/)

Tavenem.Blazor.IndexedDB
==

Tavenem.Blazor.IndexedDB is a [Razor class library](https://docs.microsoft.com/en-us/aspnet/core/razor-pages/ui-class) (RCL) containing a [Razor component](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries).
It grants managed access to the [IndexedDB API](https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API).

It uses the [idb](https://github.com/jakearchibald/idb) javascript library by [Jake Archibald](https://github.com/jakearchibald), and implements the `IDataStore` interface from the Tavenem [DataStore library](https://github.com/Tavenem/DataStore).

## Installation

Tavenem.Blazor.IndexedDB is available as a [NuGet package](https://www.nuget.org/packages/Tavenem.Blazor.IndexedDB/).

## Use

1. Construct an `IndexedDb` object to define the characteristics of your database.

    ```c#
    // simple
    var db = new IndexedDb("myDatabaseName", 1);
    
    // all options
    var db = new IndexedDb<int>(
        databaseName: "myDatabaseName",
        version: 2,
        storeName: "valueStore");
    ```

    This object can be a static instance, or you can construct instances dynamically as needed.

1. Call `AddIndexedDb(db)` with an `IndexedDb` instance, or `AddIndexedDb(provider => GetMyDatabase())` with a function that supplies one through dependency injection.

1. Inject the `IndexedDbService` instance in a component.

1. Call the `StoreItemAsync<T>`, `GetItemAsync<T>`, and `RemoveItemAsync<T>` methods to work with strongly-typed data items.

    ```c#
    class Item : IdItem
    {
        public int Id { get; set; }
        public string? Value { get; set; }
    }
    
    var item = new Item
    {
        Value = "Hello, World!",
    };
    
    await IndexedDbService.StoreItemAsync(item);
    
    item.Value = "Goodbye!";
    await IndexedDbService.StoreItemAsync(item);
    
    var fetchedItem = await IndexedDbService.GetItemAsync<Item>(item.Id);
    // fetchedItem is an Item instance: item.Value = "Goodbye!"
    
    await IndexedDbService.RemoveItemAsync(item);
    
    fetchedItem = await IndexedDbService.GetItemAsync<Item>(item.Id);
    // fetchedItem is null
    ```

1. Call the `Query<T>` method to obtain an `IDataStoreQueryable<T>`. `IDataStoreQueryable<T>` is similar to `IQueryable<T>`, and can be used to make queries against the data source.

    ```c#
    await foreach (var item in IndexedDbService.Query().AsAsyncEnumerable())
    {
        Console.WriteLine(item.Value);
    }

    var helloCount = await IndexedDbService
        .Query()
        .Select(x => x.Value != null && x.Value.Contains("Hello"))
        .CountAsync();
    ```

1. Call the `ClearAsync`, `CountAsync`, `DeleteDatabaseAsync`, and `GetAllAsync<T>` methods to work with the full database.

    ```c#
    await IndexedDbService.StoreItemAsync(item);
    
    var count = await IndexedDbService.CountAsync();
    // count = 1

    var items = await IndexedDbService.GetAllAsync<Item>();
    // items is an array of Items with Length 1

    await IndexedDbService.ClearAsync();
    count = await IndexedDbService.CountAsync();
    // count = 0

    await IndexedDbService.DeleteDatabaseAsync();
    // the database has been removed (or will be, after all connections are closed)
    ```

## Roadmap

Tavenem.IndexedDb v2.0 is currently in a prerelease state. Development is ongoing, and breaking changes are possible before the production release.

No release date is currently set for v2.0 of Tavenem.IndexedDb. The project is currently in a "wait and see" phase while Tavenem.DataStore is in prerelease. When that project has a stable release, a production release of Tavenem.IndexedDb v2.0 will follow.

In the meantime, v1 of Tavenem.IndexedDb is stable and will not undergo any breaking changes. It has a significantly different API. See the v1 branch's README for details.

## Contributing

Contributions are always welcome. Please carefully read the [contributing](docs/CONTRIBUTING.md) document to learn more before submitting issues or pull requests.

## Code of conduct

Please read the [code of conduct](docs/CODE_OF_CONDUCT.md) before engaging with our community, including but not limited to submitting or replying to an issue or pull request.