![build](https://img.shields.io/github/workflow/status/Tavenem/Blazor.IndexedDB/publish/main) [![NuGet downloads](https://img.shields.io/nuget/dt/Tavenem.Blazor.IndexedDB)](https://www.nuget.org/packages/Tavenem.Blazor.IndexedDB/)

Tavenem.Blazor.IndexedDB
==

Tavenem.Blazor.IndexedDB is a [Razor class
library](https://docs.microsoft.com/en-us/aspnet/core/razor-pages/ui-class) (RCL) containing a
[Razor component](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries).
It grants managed access to the [IndexedDB API](https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API).

It is a simple wrapper for the [idb](https://github.com/jakearchibald/idb) javascript library by [Jake Archibald](https://github.com/jakearchibald).

## Installation

Tavenem.Blazor.IndexedDB is available as a [NuGet package](https://www.nuget.org/packages/Tavenem.Blazor.IndexedDB/).

## Use

1. Construct an `IndexedDb<TKey>` object to define the characteristics of your database.

    ```c#
    // simple
    var db = new IndexedDb<int>("myDatabaseName", 1);
    
    // all options
    var db = new IndexedDb<int>(
        name: "myDatabaseName",
        version: 2,
        keyPath: "Id",
        storeName: "valueStore");
    ```

    This object can be a static instance, or you can construct instances dynamically as needed.

1. Call the `AddIndexedDb()` extension method on your `IServiceCollection`.

    **or**

    Call `AddIndexedDb(db)` with an `IndexedDb<TKey>` instance, or `AddIndexedDb(provider => GetMyDatabase())` with a function that supplies one through dependency injection.
    These will register a strongly-typed service.

1. Inject the `IndexedDbService` instance in a component.

    **or**

    Inject the strongly-typed `IndexedDbService<TKey>` instance, if you used one of the strongly-typed extensions in the previous step.

1. Call the `AddValueAsync`, `PutValueAsync`, `GetValueAsync`, and `DeleteValueAsync` methods to work with strongly-typed data items.

    ```c#
    class Item
    {
        public int Id { get; set; }
        public string? Value { get; set; }
    }
    
    var item = new Item
    {
        Id = 1,
        Value = "Hello, World!",
    };
    
    await IndexedDbService.AddValueAsync(db, item);
    
    item.Value = "Goodbye!";
    await IndexedDbService.PutValueAsync(db, item);
    
    var fetchedItem = await IndexedDbService.GetValueAsync<int, Item>(db, 1);
    // fetchedItem is an Item instance: item.Id = 1, item.Value = "Goodbye!"
    
    await IndexedDbService.DeleteValueAsync(db, item);
    // or await IndexedDbService.DeleteKeyAsync(db, 1);
    
    fetchedItem = await IndexedDbService.GetValueAsync<int, Item>(db, 1);
    // fetchedItem is null
    ```
    
    If you are using a strongly-typed service, you can omit the first `IndexedDb<TKey>` parameter. The database you assigned during service registration will be used automatically.

    If you are using the un-typed service, you must supply the `IndexedDb<TKey>` object as the first parameter (as shown above), which tells them which database and object store to use.

1. Call the `ClearAsync`, `CountAsync`, `DeleteDatabaseAsync`, and `GetAllAsync` methods to work with the full database.

    ```c#
    await IndexedDbService.AddValueAsync(db, item);
    
    var count = await IndexedDbService.CountAsync(db, item);
    // count = 1

    var items = await IndexedDbService.GetAllAsync(db, item);
    // items is an array of Items with Length 1

    await IndexedDbService.ClearAsync(db);
    count = await IndexedDbService.CountAsync(db, item);
    // count = 0

    await IndexedDbService.DeleteDatabaseAsync(db, item);
    // the database has been removed (or will be, after all connections are closed)
    ```

    As before, you can omit the database parameter if you are using a strongly-typed service.

The strongly-typed service is more convenient if your app will be using the same database throughout, and the database options are not expected to change during the app's operation.

The un-typed service can be helpful if your app will use various databases, or if the database options are expected to change.

## Roadmap

No specific updates are planned for Tavenem.Blazor.IndexedDB, although bugfixes are always possible.

## Contributing

Contributions are always welcome. Please carefully read the [contributing](docs/CONTRIBUTING.md) document to learn more before submitting issues or pull requests.

## Code of conduct

Please read the [code of conduct](docs/CODE_OF_CONDUCT.md) before engaging with our community, including but not limited to submitting or replying to an issue or pull request.