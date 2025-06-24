using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.Versioning;
using System.Text.Json.Serialization.Metadata;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Provides LINQ operations on an <see cref="IndexedDbService"/>.
/// </summary>
/// <remarks>
/// Note: this class does not implement the synchronous methods of <see cref="IDataStore"/>.
/// Synchronous access to an IDBObjectStore from Blazor is not supported. Always use the equivalent
/// asynchronous methods.
/// </remarks>
public class IndexedDbQueryable<T> : IDataStoreQueryable<T>
{
    private const string SyncNotSupportedMessage = "This method is not supported by this library. Please use the async version of this method.";

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

    /// <inheritdoc/>
    /// <remarks>
    /// Always throws <see cref="NotImplementedException"/>. Use the async version of this method.
    /// </remarks>
    /// <exception cref="NotImplementedException" />
    [DoesNotReturn, UnsupportedOSPlatform("browser")]
    public bool Any() => throw new NotImplementedException(SyncNotSupportedMessage);

    /// <inheritdoc/>
    /// <remarks>
    /// Always throws <see cref="NotImplementedException"/>. Use the async version of this method.
    /// </remarks>
    /// <exception cref="NotImplementedException" />
    [DoesNotReturn, UnsupportedOSPlatform("browser")]
    public bool Any(Expression<Func<T, bool>> predicate) => throw new NotImplementedException(SyncNotSupportedMessage);

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

    /// <inheritdoc/>
    /// <remarks>
    /// Always throws <see cref="NotImplementedException"/>. Use the async version of this method.
    /// </remarks>
    /// <exception cref="NotImplementedException" />
    [DoesNotReturn, UnsupportedOSPlatform("browser")]
    public IEnumerable<T> AsEnumerable() => throw new NotImplementedException(SyncNotSupportedMessage);

    /// <inheritdoc/>
    /// <remarks>
    /// Always throws <see cref="NotImplementedException"/>. Use the async version of this method.
    /// </remarks>
    /// <exception cref="NotImplementedException" />
    [DoesNotReturn, UnsupportedOSPlatform("browser")]
    public int Count() => throw new NotImplementedException(SyncNotSupportedMessage);

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

    /// <inheritdoc/>
    /// <remarks>
    /// Always throws <see cref="NotImplementedException"/>. Use the async version of this method.
    /// </remarks>
    /// <exception cref="NotImplementedException" />
    [DoesNotReturn, UnsupportedOSPlatform("browser")]
    public T? FirstOrDefault() => throw new NotImplementedException(SyncNotSupportedMessage);

    /// <inheritdoc/>
    /// <remarks>
    /// Always throws <see cref="NotImplementedException"/>. Use the async version of this method.
    /// </remarks>
    /// <exception cref="NotImplementedException" />
    [DoesNotReturn, UnsupportedOSPlatform("browser")]
    public T? FirstOrDefault(Expression<Func<T, bool>> predicate) => throw new NotImplementedException(SyncNotSupportedMessage);

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

    /// <inheritdoc/>
    /// <remarks>
    /// Always throws <see cref="NotImplementedException"/>. Use the async version of this method.
    /// </remarks>
    /// <exception cref="NotImplementedException" />
    [DoesNotReturn, UnsupportedOSPlatform("browser")]
    public IPagedList<T> GetPage(int pageNumber, int pageSize) => throw new NotImplementedException(SyncNotSupportedMessage);

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

    /// <inheritdoc/>
    /// <remarks>
    /// Always throws <see cref="NotImplementedException"/>. Use the async version of this method.
    /// </remarks>
    /// <exception cref="NotImplementedException" />
    [DoesNotReturn, UnsupportedOSPlatform("browser")]
    public T? Max() => throw new NotImplementedException(SyncNotSupportedMessage);

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

    /// <inheritdoc/>
    /// <remarks>
    /// Always throws <see cref="NotImplementedException"/>. Use the async version of this method.
    /// </remarks>
    /// <exception cref="NotImplementedException" />
    [DoesNotReturn, UnsupportedOSPlatform("browser")]
    public T? Min() => throw new NotImplementedException(SyncNotSupportedMessage);

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

    /// <inheritdoc/>
    /// <remarks>
    /// Always throws <see cref="NotImplementedException"/>. Use the async version of this method.
    /// </remarks>
    /// <exception cref="NotImplementedException" />
    [DoesNotReturn, UnsupportedOSPlatform("browser")]
    public IReadOnlyList<T> ToList() => throw new NotImplementedException(SyncNotSupportedMessage);

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
                || batchCount < 20)
            {
                break;
            }
            reset = false;
        }
    }
}
