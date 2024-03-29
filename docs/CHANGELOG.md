# Changelog

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