namespace Tavenem.Blazor.IndexedDB;

internal record BatchResult<T>(List<T> Items, bool More);
