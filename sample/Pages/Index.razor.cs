using Microsoft.AspNetCore.Components;

namespace Tavenem.Blazor.IndexedDB.Sample.Pages;

public partial class Index
{
    private long Count { get; set; }

    [Inject] private IndexedDbService<string>? IndexedDbService { get; set; }

    private List<Item> Items { get; set; } = new();

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
        if (IndexedDbService is null)
        {
            Console.WriteLine("null service");
            return;
        }
        if (string.IsNullOrWhiteSpace(Value))
        {
            return;
        }

        var item = new Item
        {
            Id = Guid.NewGuid().ToString(),
            Value = Value
        };
        Value = null;

        await IndexedDbService
            .AddValueAsync(item)
            .ConfigureAwait(false);

        Items.Add(item);
        Count++;
    }

    private async Task OnClearAsync()
    {
        if (IndexedDbService is null)
        {
            Console.WriteLine("null service");
            return;
        }

        await IndexedDbService
            .ClearAsync()
            .ConfigureAwait(false);

        Items.Clear();
        Count = 0;
    }

    private async Task OnDeleteAsync(Item item)
    {
        if (IndexedDbService is null)
        {
            Console.WriteLine("null service");
            return;
        }

        await IndexedDbService
            .DeleteKeyAsync(item.Id)
            .ConfigureAwait(false);

        Items.Remove(item);
        Count--;
    }

    private async Task OnDeleteDatabaseAsync()
    {
        if (IndexedDbService is null)
        {
            Console.WriteLine("null service");
            return;
        }

        await IndexedDbService
            .DeleteDatabaseAsync()
            .ConfigureAwait(false);

        Count = 0;
        Items.Clear();
    }

    private async Task OnRefreshAsync()
    {
        if (IndexedDbService is null)
        {
            Console.WriteLine("null service");
            return;
        }

        Count = await IndexedDbService
            .CountAsync()
            .ConfigureAwait(false);

        Items = (await IndexedDbService
            .GetAllAsync<Item>()
            .ConfigureAwait(false))?
            .ToList()
            ?? new();
    }

    private async Task OnRefreshItemAsync(Item item)
    {
        if (IndexedDbService is null)
        {
            Console.WriteLine("null service");
            return;
        }

        var index = Items.IndexOf(item);
        if (index == -1)
        {
            return;
        }

        var newItem = await IndexedDbService
            .GetValueAsync<Item>(item.Id)
            .ConfigureAwait(false);
        if (newItem is not null)
        {
            Items.RemoveAt(index);
            Items.Insert(index, newItem);
        }
    }

    private async Task OnUpdateAsync(Item item)
    {
        if (IndexedDbService is null)
        {
            Console.WriteLine("null service");
            return;
        }

        item.Value = item.NewValue;
        item.IsUpdating = false;

        await IndexedDbService
            .PutValueAsync(item)
            .ConfigureAwait(false);
    }
}
