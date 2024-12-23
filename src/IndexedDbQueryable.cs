using System.Linq.Expressions;
using System.Text.Json.Serialization.Metadata;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Provides LINQ operations on an <see cref="IndexedDbService"/>.
/// </summary>
public class IndexedDbQueryable<T> : IDataStoreQueryable<T>
{
    private protected readonly Expression<Func<T, bool>>? _conditionalExpression;
    private readonly IndexedDbStore? _store;
    private protected readonly int _skip = 0;
    private protected readonly int _take = -1;
    private protected readonly JsonTypeInfo<T>? _typeInfo;

    /// <summary>
    /// Constructs a new instance of <see cref="IndexedDbQueryable{T}"/>.
    /// </summary>
    public IndexedDbQueryable(IndexedDbStore? store, JsonTypeInfo<T>? typeInfo = null)
    {
        _store = store;
        _typeInfo = typeInfo;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="IndexedDbQueryable{T}"/>.
    /// </summary>
    protected IndexedDbQueryable(JsonTypeInfo<T>? typeInfo = null) => _typeInfo = typeInfo;

    /// <summary>
    /// Constructs a new instance of <see cref="IndexedDbQueryable{T}"/>.
    /// </summary>
    protected IndexedDbQueryable(int skip, int take, JsonTypeInfo<T>? typeInfo = null)
    {
        _skip = skip;
        _take = take;
        _typeInfo = typeInfo;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="IndexedDbQueryable{T}"/>.
    /// </summary>
    protected IndexedDbQueryable(Expression<Func<T, bool>>? expression, int skip, int take, JsonTypeInfo<T>? typeInfo = null)
    {
        _conditionalExpression = expression;
        _skip = skip;
        _take = take;
        _typeInfo = typeInfo;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="IndexedDbQueryable{T}"/>.
    /// </summary>
    private IndexedDbQueryable(
        IndexedDbStore? store,
        Expression<Func<T, bool>>? expression,
        int skip,
        int take,
        JsonTypeInfo<T>? typeInfo = null)
    {
        _conditionalExpression = expression;
        _store = store;
        _skip = skip;
        _take = take;
        _typeInfo = typeInfo;
    }

    /// <summary>
    /// Determines whether this <see cref="IDataStoreQueryable{T}" /> contains any elements.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the source sequence contains any elements; otherwise, <see
    /// langword="false" />.
    /// </returns>
    /// <remarks>
    /// This method blocks on <see cref="AnyAsync()"/>. Always use that method instead to avoid
    /// issues.
    /// </remarks>
    public bool Any() => AnyAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Determines whether any element of this <see cref="IDataStoreQueryable{T}" /> satisfies a
    /// condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    /// <see langword="true" /> if any elements in the source sequence pass the test in the
    /// specified predicate; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This method blocks on <see cref="AnyAsync(Expression{Func{T, bool}})"/>. Always use that
    /// method instead to avoid issues.
    /// </remarks>
    public bool Any(Expression<Func<T, bool>> predicate) => AnyAsync(predicate).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<bool> AnyAsync()
    {
        await foreach (var _ in IterateSourceAsync())
        {
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        var test = predicate.Compile();
        await foreach (var item in IterateSourceAsync())
        {
            if (test.Invoke(item))
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(Func<T, ValueTask<bool>> predicate)
    {
        await foreach (var item in IterateSourceAsync())
        {
            if (await predicate.Invoke(item))
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<T> AsAsyncEnumerable() => IterateSourceAsync();

    /// <summary>
    /// Enumerates the results of this <see cref="IDataStoreQueryable{T}" />.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}" />.</returns>
    /// <remarks>
    /// This method blocks on an asynchronous interop call. Always use <see
    /// cref="AsAsyncEnumerable"/> instead to avoid issues.
    /// </remarks>
    public IEnumerable<T> AsEnumerable() => IterateSource();

    /// <summary>
    /// Returns the number of elements in this <see cref="IDataStoreQueryable{T}" />.
    /// </summary>
    /// <returns>The number of elements in this <see cref="IDataStoreQueryable{T}" />.</returns>
    /// <exception cref="OverflowException">
    /// The number of elements in source is larger than <see cref="F:System.Int32.MaxValue" />.
    /// </exception>
    /// <remarks>
    /// This method blocks on <see cref="CountAsync"/>. Always use that method instead to avoid
    /// issues.
    /// </remarks>
    public int Count() => CountAsync().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<int> CountAsync()
    {
        var count = 0;
        await foreach (var item in IterateSourceAsync())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Returns the first element of this <see cref="IDataStoreQueryable{T}" />, or a default value
    /// if the sequence contains no elements.
    /// </summary>
    /// <returns>
    /// The first element in this <see cref="IDataStoreQueryable{T}" />, or a default value if the
    /// sequence contains no elements.
    /// </returns>
    /// <remarks>
    /// This method blocks on <see cref="FirstOrDefaultAsync()"/>. Always use that method instead to
    /// avoid issues.
    /// </remarks>
    public T? FirstOrDefault() => FirstOrDefaultAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Returns the first element of this <see cref="IDataStoreQueryable{T}" /> that satisfies a
    /// specified condition or a default value if no such element is found.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    /// default(TSource) if this <see cref="IDataStoreQueryable{T}" /> is empty or if no element
    /// passes the test specified by <paramref name="predicate" />; otherwise, the first element in
    /// source that passes the test specified by <paramref name="predicate" />.
    /// </returns>
    /// <remarks>
    /// This method blocks on <see cref="FirstOrDefaultAsync(Expression{Func{T, bool}})"/>. Always
    /// use that method instead to avoid issues.
    /// </remarks>
    public T? FirstOrDefault(Expression<Func<T, bool>> predicate)
        => FirstOrDefaultAsync(predicate).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<T?> FirstOrDefaultAsync()
    {
        await foreach (var item in IterateSourceAsync())
        {
            return item;
        }
        return default;
    }

    /// <inheritdoc/>
    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        var test = predicate.Compile();
        await foreach (var item in IterateSourceAsync())
        {
            if (test.Invoke(item))
            {
                return item;
            }
        }
        return default;
    }

    /// <inheritdoc/>
    public async Task<T?> FirstOrDefaultAsync(Func<T, ValueTask<bool>> predicate)
    {
        await foreach (var item in IterateSourceAsync())
        {
            if (await predicate.Invoke(item))
            {
                return item;
            }
        }
        return default;
    }

    /// <summary>
    /// Gets a number of items from this <see cref="IDataStoreQueryable{T}" /> equal to <paramref
    /// name="pageSize" />, after skipping <paramref name="pageNumber" />-1 multiples of that
    /// amount.
    /// </summary>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>
    /// An <see cref="IPagedList{T}" /> of items from this <see cref="IDataStoreQueryable{T}" />.
    /// </returns>
    /// <remarks>
    /// This method blocks on <see cref="GetPageAsync(int, int)"/>. Always use that method instead
    /// to avoid issues.
    /// </remarks>
    public IPagedList<T> GetPage(int pageNumber, int pageSize)
        => GetPageAsync(pageNumber, pageSize).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<IPagedList<T>> GetPageAsync(int pageNumber, int pageSize)
    {
        var list = new List<T>();
        var total = 0;
        var skip = (pageNumber - 1) * pageSize;
        await foreach (var item in IterateSourceAsync())
        {
            total++;
            if (total <= skip)
            {
                continue;
            }
            if (list.Count < pageSize)
            {
                list.Add(item);
            }
        }
        return new PagedList<T>(list, pageNumber, pageSize, total);
    }

    /// <summary>
    /// Returns the maximum value of this <see cref="IDataStoreQueryable{T}" />.
    /// </summary>
    /// <returns>The maximum value of this <see cref="IDataStoreQueryable{T}" />.</returns>
    /// <remarks>
    /// This method blocks on <see cref="MaxAsync"/>. Always use that method instead to avoid
    /// issues.
    /// </remarks>
    public T? Max() => MaxAsync().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<T?> MaxAsync()
    {
        T? max = default;
        await foreach (var item in IterateSourceAsync())
        {
            if (max is null
                || Comparer<T>.Default.Compare(item, max) > 0)
            {
                max = item;
            }
        }
        return max;
    }

    /// <summary>
    /// Returns the minimum value of this <see cref="IDataStoreQueryable{T}" />.
    /// </summary>
    /// <returns>The minimum value of this <see cref="IDataStoreQueryable{T}" />.</returns>
    /// <remarks>
    /// This method blocks on <see cref="MinAsync"/>. Always use that method instead to avoid
    /// issues.
    /// </remarks>
    public T? Min() => MinAsync().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<T?> MinAsync()
    {
        T? min = default;
        await foreach (var item in IterateSourceAsync())
        {
            if (min is null
                || Comparer<T>.Default.Compare(item, min) < 0)
            {
                min = item;
            }
        }
        return min;
    }

    /// <inheritdoc/>
    public IDataStoreQueryable<TResult> OfType<TResult>(JsonTypeInfo<TResult>? typeInfo = null)
        => Where(x => x is TResult)
        .Select(x => (TResult)(object)x!, typeInfo);

    /// <inheritdoc/>
    public IOrderedDataStoreQueryable<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector, bool descending = false)
        => new OrderedIndexedDbQueryable<T, TKey>(this, keySelector, descending, _typeInfo);

    /// <inheritdoc/>
    public IDataStoreQueryable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector, JsonTypeInfo<TResult>? typeInfo = null)
        => new SelectedIndexedDbQueryable<TResult, T>(this, selector, typeInfo);

    /// <inheritdoc/>
    public async IAsyncEnumerable<TResult> SelectAsync<TResult>(Func<T, ValueTask<TResult>> selector, JsonTypeInfo<TResult>? typeInfo = null)
    {
        await foreach (var item in IterateSourceAsync())
        {
            yield return await selector.Invoke(item);
        }
    }

    /// <inheritdoc/>
    public IDataStoreQueryable<TResult> SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector, JsonTypeInfo<TResult>? typeInfo = null)
        => new SelectedIndexedDbQueryable<TResult, T>(this, selector, typeInfo);

    /// <inheritdoc/>
    public IDataStoreQueryable<TResult> SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector,
        JsonTypeInfo<TResult>? typeInfo = null) => new ManySelectedIndexedDbQueryable<TResult, TCollection, T>(
        this,
        collectionSelector,
        resultSelector,
        typeInfo);

    /// <inheritdoc/>
    public async IAsyncEnumerable<TResult> SelectManyAsync<TResult>(Func<T, IAsyncEnumerable<TResult>> selector, JsonTypeInfo<TResult>? typeInfo = null)
    {
        await foreach (var item in IterateSourceAsync())
        {
            await foreach (var child in selector.Invoke(item))
            {
                yield return child;
            }
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TResult> SelectManyAsync<TCollection, TResult>(
        Func<T, IEnumerable<TCollection>> collectionSelector,
        Func<T, TCollection, ValueTask<TResult>> resultSelector,
        JsonTypeInfo<TResult>? typeInfo = null)
    {
        await foreach (var item in IterateSourceAsync())
        {
            foreach (var child in collectionSelector.Invoke(item))
            {
                yield return await resultSelector.Invoke(item, child);
            }
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TResult> SelectManyAsync<TCollection, TResult>(
        Func<T, IAsyncEnumerable<TCollection>> collectionSelector,
        Func<T, TCollection, TResult> resultSelector,
        JsonTypeInfo<TResult>? typeInfo = null)
    {
        await foreach (var item in IterateSourceAsync())
        {
            await foreach (var child in collectionSelector.Invoke(item))
            {
                yield return resultSelector.Invoke(item, child);
            }
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TResult> SelectManyAsync<TCollection, TResult>(
        Func<T, IAsyncEnumerable<TCollection>> collectionSelector,
        Func<T, TCollection, ValueTask<TResult>> resultSelector,
        JsonTypeInfo<TResult>? typeInfo = null)
    {
        await foreach (var item in IterateSourceAsync())
        {
            await foreach (var child in collectionSelector.Invoke(item))
            {
                yield return await resultSelector.Invoke(item, child);
            }
        }
    }

    /// <inheritdoc/>
    public virtual IDataStoreQueryable<T> Skip(int count) => new IndexedDbQueryable<T>(
        _store,
        _conditionalExpression,
        count,
        _take,
        _typeInfo);

    /// <inheritdoc/>
    public virtual IDataStoreQueryable<T> Take(int count) => new IndexedDbQueryable<T>(
        _store,
        _conditionalExpression,
        _skip,
        count,
        _typeInfo);

    /// <summary>
    /// Enumerates the results of this <see cref="IDataStoreQueryable{T}" /> and returns them as
    /// a <see cref="IReadOnlyList{T}" />.
    /// </summary>
    /// <returns>A <see cref="IReadOnlyList{T}" />.</returns>
    /// <remarks>
    /// This method blocks on <see cref="ToListAsync"/>. Always use that method instead to avoid
    /// issues.
    /// </remarks>
    public IReadOnlyList<T> ToList() => ToListAsync().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<IReadOnlyList<T>> ToListAsync()
    {
        var list = new List<T>();
        await foreach (var item in IterateSourceAsync())
        {
            list.Add(item);
        }
        return list;
    }

    /// <inheritdoc/>
    public virtual IDataStoreQueryable<T> Where(Expression<Func<T, bool>> predicate) => new IndexedDbQueryable<T>(
        _store,
        _conditionalExpression is null
            ? predicate
            : CombineCondition(predicate),
        _skip,
        _take,
        _typeInfo);

    /// <inheritdoc/>
    public async IAsyncEnumerable<T> WhereAsync(Func<T, ValueTask<bool>> predicate)
    {
        await foreach (var item in IterateSourceAsync())
        {
            if (await predicate.Invoke(item))
            {
                yield return item;
            }
        }
    }

    private protected Expression<Func<T, bool>> CombineCondition(Expression<Func<T, bool>> expression)
    {
        if (_conditionalExpression is null)
        {
            return expression;
        }

        var newExpression = new ReplaceExpressionVisitor(
            expression.Parameters[0],
            _conditionalExpression.Parameters[0])
            .Visit(expression.Body)
            ?? throw new InvalidOperationException("Expression could not be constructed successfully.");
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(
            _conditionalExpression,
            newExpression),
            _conditionalExpression.Parameters[0]);
    }

    internal virtual IEnumerable<T> IterateSource()
    {
        if (_store is null)
        {
            yield break;
        }
        var condition = _conditionalExpression?.Compile();
        var reset = true;
        var count = 0;
        while (true)
        {
            var batchCount = 0;
            var enumerator = _store
                .GetBatchAsync(reset, _typeInfo)
                .GetAsyncEnumerator();
            while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
            {
                batchCount++;
                if (condition?.Invoke(enumerator.Current) == false)
                {
                    continue;
                }
                count++;
                if (count <= _skip)
                {
                    continue;
                }
                yield return enumerator.Current;
                if (_take >= 0 && count >= _take)
                {
                    break;
                }
            }
            if (batchCount == 0)
            {
                break;
            }
            if ((_take >= 0 && count >= _take)
                || batchCount < int.MaxValue)
            {
                break;
            }
            reset = false;
        }
    }

    internal virtual async IAsyncEnumerable<T> IterateSourceAsync()
    {
        if (_store is null)
        {
            yield break;
        }
        var condition = _conditionalExpression?.Compile();
        var reset = true;
        var count = 0;
        while (true)
        {
            var batchCount = 0;
            await foreach (var item in _store
                .GetBatchAsync(reset, _typeInfo))
            {
                batchCount++;
                if (condition?.Invoke(item) == false)
                {
                    continue;
                }
                count++;
                if (count <= _skip)
                {
                    continue;
                }
                yield return item;
                if (_take >= 0 && count >= _take)
                {
                    break;
                }
            }
            if (batchCount == 0)
            {
                break;
            }
            if ((_take >= 0 && count >= _take)
                || batchCount < int.MaxValue)
            {
                break;
            }
            reset = false;
        }
    }
}
