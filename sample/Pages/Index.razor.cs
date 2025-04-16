using Microsoft.AspNetCore.Components;

namespace Tavenem.Blazor.IndexedDB.Sample.Pages;

public partial class Index
{
    public const string StoreName = "SampleStore";

    private long Count { get; set; }

    private string? Filter { get; set; }

    private int FilterCount { get; set; }

    private bool FilterMatched { get; set; }

    private List<Item> FilteredItems { get; set; } = [];

    [Inject(Key = "Tavenem.Blazor.IndexedDB.Sample")] private IndexedDb IndexedDb { get; set; } = default!;

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

        var store = IndexedDb[StoreName];
        if (store is not null && await store.StoreItemAsync(item))
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
        var store = IndexedDb[StoreName];
        if (store is not null)
        {
            await store.ClearAsync();
        }
        Items.Clear();
        FilteredItems.Clear();
        Count = 0;
        FilterMatched = false;
        FilterCount = 0;
    }

    private async Task OnDeleteAsync(Item item)
    {
        var store = IndexedDb[StoreName];
        if (store is null || await store.RemoveItemAsync(item.Id))
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

        var store = IndexedDb[StoreName];
        if (store is null)
        {
            FilterMatched = false;
            FilterCount = 0;
            FilteredItems = [];
            return;
        }

        var query = store
            .Query<Item>()
            .Where(x => x.Value != null
                && x.Value.Contains(Filter, StringComparison.OrdinalIgnoreCase));

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
        Items = [];

        var store = IndexedDb[StoreName];
        if (store is null)
        {
            Count = 0;
            return;
        }

        Count = await store.CountAsync();
        await foreach (var item in store.GetAllAsync<Item>())
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

        var store = IndexedDb[StoreName];
        if (store is null)
        {
            return;
        }
        var newItem = await store.GetItemAsync<Item>(item.Id);
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

        var store = IndexedDb[StoreName];
        if (store is not null
            && !await store.StoreItemAsync(item)
            && await store.GetItemAsync<Item>(item.Id) is Item oldItem)
        {
            item.Value = oldItem.Value;
            item.NewValue = oldItem.Value;
        }
    }
}
