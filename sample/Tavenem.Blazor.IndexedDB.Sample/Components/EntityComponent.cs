using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Tavenem.Blazor.IndexedDB.Sample.Entities;
using Tavenem.Blazor.IndexedDB.Sample.Models;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB.Sample.Components;

public class EntityComponent<T> : ComponentBase where T : EntityBase, new()
{
    private string _storeName;

    protected long Count { get; set; }

    protected int FilterCount { get; set; }

    protected bool FilterMatched { get; set; }

    protected List<T> FilteredItems { get; set; } = [];

    protected List<T> Items { get; set; } = [];

    protected bool IsLoading { get; set; }

    protected T NewItem { get; set; } = new T();

    protected string? Filter { get; set; }

    [NotNull, Inject(Key = DatabaseContext.DatabaseName)] 
    private IndexedDb? IndexedDb { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _storeName = $"{typeof(T).Name}s";

        await OnRefreshAsync();
    }

    protected void OnCancelEdit(T item)
    {
        item.IsEditing = false;
    }

    protected void OnEdit(T item)
    {
        item.IsEditing = true;
    }

    protected async Task OnAddAsync(T item)
    {
        if (item.IsEditing)
            await OnUpdateAsync(item);

        else if (await IndexedDb[_storeName].StoreItemAsync(item))
        {
            Items.Add(item);

            if (Filter is null
                || item.Id.Contains(Filter, StringComparison.OrdinalIgnoreCase))
            {
                FilteredItems.Add(item);
                FilteredItems.Sort((x, y) => x.Id?.CompareTo(y.Id) ?? (y.Id is null ? 0 : -1));
            }

            Count++;

            NewItem = new T();
        }
    }

    protected async Task OnClearAsync()
    {
        await IndexedDb[_storeName].ClearAsync();
        Items.Clear();
        FilteredItems.Clear();
        Count = 0;
        FilterMatched = false;
        FilterCount = 0;
    }

    protected async Task OnDeleteAsync(T item)
    {
        if (await IndexedDb[_storeName].RemoveItemAsync(item.Id))
        {
            Items.Remove(item);
            FilteredItems.Remove(item);
            Count--;
        }
    }

    protected async Task OnFilterAsync()
    {
        if (string.IsNullOrEmpty(Filter))
        {
            FilteredItems = Items;
            FilterMatched = false;
            FilterCount = 0;
            return;
        }

        var query = IndexedDb[_storeName]
            .Query<T>()
            .Where(x => x.Id != null && x.Id.Contains(Filter, StringComparison.OrdinalIgnoreCase));

        // deliberately using inefficient logic, in order to test more paths
        FilterMatched = await query.AnyAsync();

        FilterCount = await query.CountAsync();

        FilteredItems =
        [
            .. (await query
                .OrderBy(x => x.Id)
                .ToListAsync())
        ];
    }

    protected async Task OnRefreshAsync()
    {
        IsLoading = true;

        Count = await IndexedDb[_storeName].CountAsync();

        Items = [];

        await foreach (var item in IndexedDb[_storeName].GetAllAsync<T>())
        {
            Items.Add(item);
        }

        FilteredItems = [.. Items];

        FilteredItems.Sort((x, y) => x.Id?.CompareTo(y.Id) ?? (y.Id is null ? 0 : -1));

        IsLoading = false;

        StateHasChanged();
    }

    protected async Task OnRefreshItemAsync(T item)
    {
        var index = Items.IndexOf(item);
        if (index == -1)
        {
            return;
        }

        var newItem = await IndexedDb[_storeName].GetItemAsync<T>(item.Id);
        if (newItem is not null)
        {
            Items.RemoveAt(index);
            Items.Insert(index, newItem);
        }
    }

    protected async Task OnUpdateAsync(T item)
    {
        item.IsEditing = false;

        if (!await IndexedDb[_storeName].StoreItemAsync(item))
        {
            await IndexedDb[_storeName].GetItemAsync<Item>(item.Id);
        }
    }

}
