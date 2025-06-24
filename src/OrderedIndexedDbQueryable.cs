using System.Linq.Expressions;
using System.Text.Json.Serialization.Metadata;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Provides LINQ operations on an <see cref="IndexedDbService"/>.
/// </summary>
public sealed class OrderedIndexedDbQueryable<T, TKey> : IndexedDbQueryable<T>, IOrderedDataStoreQueryable<T>
{
    private readonly bool _descending;
    private readonly IndexedDbQueryable<T> _innerQueryable;
    private readonly Expression<Func<T, TKey>> _keySelector;

    /// <summary>
    /// Constructs a new instance of <see cref="OrderedIndexedDbQueryable{T, TKey}"/>.
    /// </summary>
    public OrderedIndexedDbQueryable(IndexedDbQueryable<T> inner, Expression<Func<T, TKey>> keySelector, bool descending = false, JsonTypeInfo<T>? typeInfo = null)
        : base(typeInfo)
    {
        _innerQueryable = inner;
        _keySelector = keySelector;
        _descending = descending;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="OrderedIndexedDbQueryable{T, TKey}"/>.
    /// </summary>
    private OrderedIndexedDbQueryable(
        IndexedDbQueryable<T> inner,
        Expression<Func<T, TKey>> keySelector,
        bool descending,
        Expression<Func<T, bool>>? conditionalExpression,
        int skip,
        int take,
        JsonTypeInfo<T>? typeInfo = null) : base(conditionalExpression, skip, take, typeInfo)
    {
        _innerQueryable = inner;
        _keySelector = keySelector;
        _descending = descending;
    }

    /// <inheritdoc/>
    public override IDataStoreQueryable<T> Skip(int count) => new OrderedIndexedDbQueryable<T, TKey>(
        _innerQueryable,
        _keySelector,
        _descending,
        _conditionalExpression,
        count,
        _take);

    /// <inheritdoc/>
    public override IDataStoreQueryable<T> Take(int count) => new OrderedIndexedDbQueryable<T, TKey>(
        _innerQueryable,
        _keySelector,
        _descending,
        _conditionalExpression,
        _skip,
        count);

    /// <inheritdoc/>
    public IOrderedDataStoreQueryable<T> ThenBy<TKey2>(Expression<Func<T, TKey2>> keySelector, bool descending = false)
        => new OrderedIndexedDbQueryable<T, TKey2>(this, keySelector, descending);

    /// <inheritdoc/>
    public override IDataStoreQueryable<T> Where(Expression<Func<T, bool>> predicate) => new OrderedIndexedDbQueryable<T, TKey>(
        _innerQueryable,
        _keySelector,
        _descending,
        _conditionalExpression is null
            ? predicate
            : CombineCondition(predicate),
        _skip,
        _take);

    internal override async IAsyncEnumerable<T> IterateSourceAsync()
    {
        var selector = _keySelector.Compile();
        var list = new List<T>();
        var condition = _conditionalExpression?.Compile();
        var count = 0;
        await foreach (var item in _innerQueryable.IterateSourceAsync())
        {
            if (condition?.Invoke(item) == false)
            {
                continue;
            }
            count++;
            if (count <= _skip)
            {
                continue;
            }
            list.Add(item);
            if (_take >= 0 && count >= _take)
            {
                break;
            }
        }
        foreach (var item in _descending
            ? list.OrderByDescending(selector)
            : list.OrderBy(selector))
        {
            yield return item;
        }
    }
}
