![build](https://img.shields.io/github/actions/workflow/status/Tavenem/Blazor.IndexedDB/publish.yml) [![NuGet downloads](https://img.shields.io/nuget/dt/Tavenem.Blazor.IndexedDB)](https://www.nuget.org/packages/Tavenem.Blazor.IndexedDB/)

Tavenem.Blazor.IndexedDB
==

Tavenem.Blazor.IndexedDB is a [Razor class library](https://docs.microsoft.com/en-us/aspnet/core/razor-pages/ui-class) (RCL) containing a [Razor component](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries).
It grants managed access to the [IndexedDB API](https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API).

It uses the [idb](https://github.com/jakearchibald/idb) javascript library by [Jake Archibald](https://github.com/jakearchibald), and implements the `IDataStore` interface from the Tavenem [DataStore library](https://github.com/Tavenem/DataStore).

## Installation

Tavenem.Blazor.IndexedDB is available as a [NuGet package](https://www.nuget.org/packages/Tavenem.Blazor.IndexedDB/).

## Use

1. Register the `IndexedDbService` with dependency injection.

    ```c#
    builder.Services.AddIndexedDbService();
    ```

1. Register one or more `IndexedDb` instances with dependency injection.

    ```c#
    // simple
    builder.Services.AddIndexedDb("myDatabaseName");
    
    // all options
    builder.Services.AddIndexedDb(
        databaseName: "myDatabaseName", // the database name
        objectStores: ["valueStore"], // the names of value stores
        version: 2, // the version number of the current database schema
        jsonSerializerOptions: options); // a JsonSerializerOptions instance
    ```

    Note that use of dependency injection for database instances is optional. They can also be
    initialized on demand with their public constructor, which requires an instance of
    `IndexedDbService`.

1. Inject an `IndexedDb` instance in a component.
    ```c#
    [Inject(Key = "myDatabaseName")] private IndexedDb MyDatabase { get; set; } = default!;
    ```

    Note that the `@inject` directive does not currently support keyed services.

1. Retrieve an `IndexedDbStore` instance by name.

    ```c#
    var store = MyDatabase["valueStore"];
    ```

1. Call the `StoreItemAsync<T>`, `GetItemAsync<T>`, and `RemoveItemAsync<T>` methods on an `IndexedDbStore` to work with strongly-typed data items.

    ```c#
    class Item : IIdItem
    {
        public string Id { get; set; }
        public string? Value { get; set; }
    }
    
    var item = new Item
    {
        Id = "1",
        Value = "Hello, World!",
    };
    
    await store.StoreItemAsync(item);
    
    item.Value = "Goodbye!";
    await store.StoreItemAsync(item);
    
    var fetchedItem = await store.GetItemAsync<Item>(item.Id);
    // fetchedItem is an Item instance: item.Value == "Goodbye!"
    
    await store.RemoveItemAsync(item);
    
    fetchedItem = await store.GetItemAsync<Item>(item.Id);
    // fetchedItem is null
    ```

1. Call the `Query<T>` method to obtain an `IDataStoreQueryable<T>`. `IDataStoreQueryable<T>` is similar to `IQueryable<T>`, and can be used to make queries against the data source.

    ```c#
    await foreach (var item in store.Query<Item>().AsAsyncEnumerable())
    {
        Console.WriteLine(item.Value);
    }

    var helloCount = await store
        .Query<Item>()
        .Select(x => x.Value != null && x.Value.Contains("Hello"))
        .CountAsync();
    ```

1. Call the `ClearAsync`, `CountAsync`, and `GetAllAsync<T>` methods to work with the full object store.

    ```c#
    await store.StoreItemAsync(item);
    
    var count = await store.CountAsync();
    // count = 1

    var items = await store.GetAllAsync<Item>();
    // items is an array of Items with Length 1

    await store.ClearAsync();
    count = await store.CountAsync();
    // count = 0
    ```

1. Call the `DeleteDatabaseAsync` method on the `IndexedDb` instance to remove the entire database.

    ```c#
    await MyDatabase.DeleteDatabaseAsync();
    // the database has been removed (or will be, after all connections are closed)
    ```

## Roadmap

New versions of Tavenem.IndexedDb should be expected whenever the API surface of the Tavenem [DataStore library](https://github.com/Tavenem/DataStore) receives an update.

Other updates to resolve bugs or add new features may occur at any time.

## Contributing

Contributions are always welcome. Please carefully read the [contributing](docs/CONTRIBUTING.md) document to learn more before submitting issues or pull requests.

## Code of conduct

Please read the [code of conduct](docs/CODE_OF_CONDUCT.md) before engaging with our community, including but not limited to submitting or replying to an issue or pull request.