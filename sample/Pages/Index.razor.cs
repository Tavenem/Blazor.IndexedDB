using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace Tavenem.Blazor.IndexedDB.Sample.Pages;

public partial class Index
{
    private const string _storeName = "SampleStore";

    private long Count { get; set; }

    private string? Filter { get; set; }

    private int FilterCount { get; set; }

    private bool FilterMatched { get; set; }

    private List<Item> FilteredItems { get; set; } = [];

    [NotNull, Inject(Key = "Tavenem.Blazor.IndexedDB.Sample")] private IndexedDb? IndexedDb { get; set; }

    private bool IsLoading { get; set; }

    private List<Item> Items { get; set; } = [];

    private string? Value { get; set; }

    protected override Task OnInitializedAsync() => OnRefreshAsync();

    private static void OnCancelEdit(Item item)
    {
        item.NewValue = item.Value;
        item.IsUpdating = false;
    }

    private static void OnEdit(Item item)
    {
        item.NewValue = item.Value;
        item.IsUpdating = true;
    }

    private async Task OnAddAsync()
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            return;
        }

        var item = new Item { Value = Value };
        Value = null;

        if (await IndexedDb[_storeName].StoreItemAsync(item))
        {
            Items.Add(item);
            if (Filter is null
                || item.Value.Contains(Filter, StringComparison.OrdinalIgnoreCase))
            {
                FilteredItems.Add(item);
                FilteredItems.Sort((x, y) => x.Value?.CompareTo(y.Value) ?? (y.Value is null ? 0 : -1));
            }
            Count++;
        }
    }

    private async Task OnClearAsync()
    {
        await IndexedDb[_storeName].ClearAsync();
        Items.Clear();
        FilteredItems.Clear();
        Count = 0;
        FilterMatched = false;
        FilterCount = 0;
    }

    private async Task OnDeleteAsync(Item item)
    {
        if (await IndexedDb[_storeName].RemoveItemAsync(item.Id))
        {
            Items.Remove(item);
            FilteredItems.Remove(item);
            Count--;
        }
    }

    private async Task OnDeleteDatabaseAsync()
    {
        IsLoading = true;
        StateHasChanged();

        await IndexedDb.DeleteDatabaseAsync();
        Count = 0;
        Items.Clear();
        FilteredItems.Clear();
        FilterMatched = false;
        FilterCount = 0;

        IsLoading = false;
        StateHasChanged();
    }

    private async Task OnFilterAsync()
    {
        if (string.IsNullOrEmpty(Filter))
        {
            FilteredItems = Items;
            FilterMatched = false;
            FilterCount = 0;
            return;
        }

        var query = IndexedDb[_storeName]
            .Query<Item>()
            .Where(x => x.Value != null && x.Value.Contains(Filter, StringComparison.OrdinalIgnoreCase));

        // deliberately using inefficient logic, in order to test more paths
        FilterMatched = await query.AnyAsync();

        FilterCount = await query.CountAsync();

        FilteredItems =
        [
            .. (await query
                .OrderBy(x => x.Value)
                .ToListAsync())
        ];
    }

    private async Task OnRefreshAsync()
    {
        Count = await IndexedDb[_storeName].CountAsync();
        Items = [];
        await foreach (var item in IndexedDb[_storeName].GetAllAsync<Item>())
        {
            Items.Add(item);
        }
        FilteredItems = [.. Items];
        FilteredItems.Sort((x, y) => x.Value?.CompareTo(y.Value) ?? (y.Value is null ? 0 : -1));
    }

    private async Task OnRefreshItemAsync(Item item)
    {
        var index = Items.IndexOf(item);
        if (index == -1)
        {
            return;
        }

        var newItem = await IndexedDb[_storeName].GetItemAsync<Item>(item.Id);
        if (newItem is not null)
        {
            Items.RemoveAt(index);
            Items.Insert(index, newItem);
        }
    }

    private async Task OnUpdateAsync(Item item)
    {
        item.Value = item.NewValue;
        item.IsUpdating = false;

        if (!await IndexedDb[_storeName].StoreItemAsync(item))
        {
            var oldItem = await IndexedDb[_storeName].GetItemAsync<Item>(item.Id);
            if (oldItem is not null)
            {
                item.Value = oldItem.Value;
                item.NewValue = oldItem.Value;
            }
        }
    }
}
