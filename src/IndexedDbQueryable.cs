using System.Linq.Expressions;
using System.Text.Json.Serialization.Metadata;
using Tavenem.DataStorage;
using Tavenem.DataStorage.Interfaces;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Provides LINQ operations on an <see cref="IndexedDbService"/>.
/// </summary>
/// <typeparam name="TItem">A shared interface for all stored items.</typeparam>
/// <typeparam name="TSource">
/// The type of the elements of the source.
/// </typeparam>
public class IndexedDbQueryable<TItem, TSource>(IndexedDbStore<TItem> store, JsonTypeInfo<TSource>? typeInfo = null)
    : IDataStoreFirstQueryable<TSource>,
    IDataStoreOfTypeQueryable<TSource>,
    IDataStoreSkipQueryable<TSource>,
    IDataStoreTakeQueryable<TSource>
    where TItem : notnull
    where TSource : TItem
{
    private protected readonly int? _skip;
    private protected readonly int? _take;

    /// <summary>
    /// The <see cref="IndexedDbStore{TItem}"/> provider for this queryable.
    /// </summary>
    public IndexedDbStore<TItem> IndexedDbProvider => store;

    /// <inheritdoc/>
    IDataStore IDataStoreQueryable<TSource>.Provider => store;

    /// <summary>
    /// Constructs a new instance of <see cref="IndexedDbQueryable{TItem, TSource}"/>.
    /// </summary>
    private IndexedDbQueryable(
        IndexedDbStore<TItem> store,
        int? skip = null,
        int? take = null,
        JsonTypeInfo<TSource>? typeInfo = null) : this(store, typeInfo)
    {
        _skip = skip;
        _take = take;
    }

    /// <inheritdoc />
    public async ValueTask<TSource> FirstAsync(CancellationToken cancellationToken = default)
    {
        var enumerator = Take(1).GetAsyncEnumerator(cancellationToken);
        var result = await enumerator.MoveNextAsync();
        if (result)
        {
            return enumerator.Current;
        }
        throw new InvalidOperationException("No elements in sequence.");
    }

    /// <inheritdoc />
    public ValueTask<TSource> FirstAsync(Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        => this.FirstAsync(predicate.Compile(), cancellationToken);

    /// <inheritdoc />
    public async ValueTask<TSource?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var enumerator = Take(1).GetAsyncEnumerator(cancellationToken);
        var result = await enumerator.MoveNextAsync();
        if (result)
        {
            return enumerator.Current;
        }
        return default;
    }

    /// <inheritdoc />
    public ValueTask<TSource?> FirstOrDefaultAsync(
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default)
        => this.FirstOrDefaultAsync(predicate.Compile(), cancellationToken);

    /// <inheritdoc />
    IAsyncEnumerator<TSource> IAsyncEnumerable<TSource>.GetAsyncEnumerator(CancellationToken cancellationToken)
        => store is null
        ? AsyncEnumerable.Empty<TSource>().GetAsyncEnumerator(cancellationToken)
        : store.GetAllBatchesAsync(_skip, _take, typeInfo, cancellationToken).GetAsyncEnumerator(cancellationToken);

    /// <inheritdoc />
    public IDataStoreOfTypeQueryable<TResult> OfType<TResult>(JsonTypeInfo<TResult>? typeInfo = null) where TResult : TSource
        => new IndexedDbQueryable<TItem, TResult>(
        store,
        _skip,
        _take,
        typeInfo);

    /// <inheritdoc />
    public async ValueTask<TSource> SingleAsync(CancellationToken cancellationToken = default)
    {
        var enumerator = Take(2).GetAsyncEnumerator(cancellationToken);
        var result = await enumerator.MoveNextAsync();
        if (!result)
        {
            throw new InvalidOperationException("No elements in sequence.");
        }
        var current = enumerator.Current;
        if (await enumerator.MoveNextAsync())
        {
            throw new InvalidOperationException("Sequence contains more than one element.");
        }
        return current;
    }

    /// <inheritdoc />
    public ValueTask<TSource> SingleAsync(
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default)
        => this.SingleAsync(predicate.Compile(), cancellationToken);

    /// <inheritdoc />
    public async ValueTask<TSource?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var enumerator = Take(2).GetAsyncEnumerator(cancellationToken);
        var result = await enumerator.MoveNextAsync();
        if (!result)
        {
            return default;
        }
        var current = enumerator.Current;
        if (await enumerator.MoveNextAsync())
        {
            throw new InvalidOperationException("Sequence contains more than one element.");
        }
        return current;
    }

    /// <inheritdoc />
    public ValueTask<TSource?> SingleOrDefaultAsync(
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default)
        => this.SingleOrDefaultAsync(predicate.Compile(), cancellationToken);

    /// <inheritdoc />
    public IDataStoreSkipQueryable<TSource> Skip(int count) => new IndexedDbQueryable<TItem, TSource>(
        store,
        count,
        _take,
        typeInfo);

    /// <inheritdoc />
    public IDataStoreTakeQueryable<TSource> Take(int count) => new IndexedDbQueryable<TItem, TSource>(
        store,
        _skip,
        count,
        typeInfo);

    /// <inheritdoc />
    public IDataStoreTakeQueryable<TSource> Take(Range range) => new IndexedDbQueryable<TItem, TSource>(
        store,
        range.Start.Value - 1,
        range.End.Value - range.Start.Value,
        typeInfo);

    /// <inheritdoc />
    public async ValueTask<(bool Success, int Count)> TryGetNonEnumeratedCountAsync(CancellationToken cancellationToken = default)
        => (true, (int)await store.CountAsync(cancellationToken));

    /// <inheritdoc />
    public async ValueTask<(bool Success, long Count)> TryGetNonEnumeratedLongCountAsync(CancellationToken cancellationToken = default)
        => (true, await store.CountAsync(cancellationToken));
}
