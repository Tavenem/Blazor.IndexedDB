# Changelog

## 7.0-preview.2
### Changed
- Now targets .NET 10
- The original `IndexedDbStore` has been divided into two separate implementations:
  - `IndexedDbStore<TItem>` which is an abstract class that allows specifying the item type for stored items
    - `IndexedDbStore<TItem>` implements the updated `IDataStore<string, TItem>` interface (see [the `Tavenem.DataStore` project](https://github.com/Tavenem/DataStore) for details)
  - `IndexedDbStore` which replicates the original by extending `IndexedDbStore<IIdItem>`
    - `IndexedDbStore` implements the updated `IIdItemDataStore` interface (see [the `Tavenem.DataStore` project](https://github.com/Tavenem/DataStore) for details)
- `IndexedDbQueryable<TItem, TSource>` implements the updated `IDataStoreFirstQueryable<TSource>`, `IDataStoreOfTypeQueryable<TSource>`, `IDataStoreSkipQueryable<TSource>`, and `IDataStoreTakeQueryable<TSource>` interfaces (see [the `Tavenem.DataStore` project](https://github.com/Tavenem/DataStore) for details)

## 6.3
### Added
- Support for customizing the `indexedDb keyPath`

## 6.2
### Fixed
- Batching bug when requesting exactly 20 items in a query

## 6.1
### Fixed
- Batching bug when requesting more than 20 items in a query

## 6.0
### Changed
- All sync-over-async calls now throw a `NotImplementedException` with an appropriate message, and are decorated with both the `DoesNotReturn` and `UnsupportedOSPlatform("browser")` attributes

## 5.1
### Fixed
- Loop when fetching more than 20 items

## 5.0
### Changed
- Registration of all object stores is now required during database creation
  - To add new stores during application lifetime, the database should be recreated with a new version number
  - (see [https://github.com/Tavenem/Blazor.IndexedDB/issues/5](https://github.com/Tavenem/Blazor.IndexedDB/issues/5))

## 4.2
### Fixed
- Does not create object store if it already exists

## 4.1
### Changed
- Multitarget .NET 8 & 9

## 4.0
This new version changes the model substantially.

Rather than have all methods for working with the database directly on the `IndexedDbService`, an `IndexedDb` instance now has a dictionary of `IndexedDbStore` objects. These represent [object stores in the Indexed DB](https://developer.mozilla.org/en-US/docs/Web/API/IDBObjectStore), and have instance methods which reflect those on the service itself.

The service is no longer limited to a single object store. The service can now instead be registered with dependency injection once. Individual database instances can be registered with dependency injection as well (or constructed directly), and are distinguished by database name. Each database can also have an unlimited number of object stores (also distinguished by name).
### Added
- `IndexedDbStore` class
  - has most methods that previously belonged to `IndexedDbService`
- `AddIndexedDbService` extension to `IServiceCollection` to register service independently of any database or object store
### Changed
- `DeleteDatabaseAsync` method added to `IndexedDb`
- most methods on `IndexedDbService` which have not been removed now require a `IndexedDbStore` instance as a parameter
- `AddIndexedDb` extension on `IServiceCollection` now registers an `IndexedDb` instance rather than the service, using a [keyed service name](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection#keyed-services) which is the same as the database name provided
### Removed
- most methods on `IndexedDbService`

## 3.0
### Added
- `JsonTypeInfo` support

## 2.3
### Fixed
- Retrieval of value with options

## 2.2
### Changed
- Improved null checking

## 2.0
### Changed
- `IndexedDb` no longer has a typed key. Only string keys are used.
- `IndexedDbService` no longer has a typed key. Only string keys are used.
- `IndexedDbService` now implements [`IDataStore`](https://github.com/Tavenem/DataStore).
  - `AddValueAsync` and `PutValueAsync` are now `StoreItemAsync<T>`
  - `GetValueAsync` is now `GetItemAsync<T>`
  - `DeleteValueAsync` is now `RemoveItemAsync<T>`

## 1.0.0
### Added
- Initial release