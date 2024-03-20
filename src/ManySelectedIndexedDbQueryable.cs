using System.Linq.Expressions;
using System.Text.Json.Serialization.Metadata;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Provides LINQ operations on an <see cref="IndexedDbService"/>.
/// </summary>
internal sealed class ManySelectedIndexedDbQueryable<T, TCollection, U> : IndexedDbQueryable<T>
{
    private readonly IndexedDbQueryable<U> _innerQueryable;
    private readonly Expression<Func<U, IEnumerable<TCollection>>> _collectionSelector;
    private readonly Expression<Func<U, TCollection, T>> _resultSelector;

    /// <summary>
    /// Constructs a new instance of <see cref="ManySelectedIndexedDbQueryable{T, TCollection, U}"/>.
    /// </summary>
    public ManySelectedIndexedDbQueryable(
        IndexedDbQueryable<U> inner,
        Expression<Func<U, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<U, TCollection, T>> resultSelector,
        JsonTypeInfo<T>? typeInfo = null) : base(typeInfo)
    {
        _innerQueryable = inner;
        _collectionSelector = collectionSelector;
        _resultSelector = resultSelector;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="ManySelectedIndexedDbQueryable{T, TCollection, U}"/>.
    /// </summary>
    private ManySelectedIndexedDbQueryable(
        IndexedDbQueryable<U> inner,
        Expression<Func<U, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<U, TCollection, T>> resultSelector,
        Expression<Func<T, bool>>? conditionalExpression,
        int skip,
        int take,
        JsonTypeInfo<T>? typeInfo = null)
        : base(conditionalExpression, skip, take, typeInfo)
    {
        _innerQueryable = inner;
        _collectionSelector = collectionSelector;
        _resultSelector = resultSelector;
    }

    /// <inheritdoc/>
    public override IDataStoreQueryable<T> Skip(int count) => new ManySelectedIndexedDbQueryable<T, TCollection, U>(
        _innerQueryable,
        _collectionSelector,
        _resultSelector,
        _conditionalExpression,
        count,
        _take);

    /// <inheritdoc/>
    public override IDataStoreQueryable<T> Take(int count) => new ManySelectedIndexedDbQueryable<T, TCollection, U>(
        _innerQueryable,
        _collectionSelector,
        _resultSelector,
        _conditionalExpression,
        _skip,
        count);

    /// <inheritdoc/>
    public override IDataStoreQueryable<T> Where(Expression<Func<T, bool>> predicate) => new ManySelectedIndexedDbQueryable<T, TCollection, U>(
        _innerQueryable,
        _collectionSelector,
        _resultSelector,
        _conditionalExpression is null
            ? predicate
            : CombineCondition(predicate),
        _skip,
        _take);

    internal override IEnumerable<T> IterateSource()
    {
        var collectionSelector = _collectionSelector.Compile();
        var resultSelector = _resultSelector.Compile();
        var condition = _conditionalExpression?.Compile();
        var count = 0;
        foreach (var item in _innerQueryable.IterateSource())
        {
            var collection = collectionSelector.Invoke(item);
            foreach (var child in collection)
            {
                var result = resultSelector.Invoke(item, child);
                if (condition?.Invoke(result) == false)
                {
                    continue;
                }
                count++;
                if (count <= _skip)
                {
                    continue;
                }
                yield return result;
                if (_take >= 0 && count >= _take)
                {
                    break;
                }
            }
        }
    }

    internal override async IAsyncEnumerable<T> IterateSourceAsync()
    {
        var collectionSelector = _collectionSelector.Compile();
        var resultSelector = _resultSelector.Compile();
        var condition = _conditionalExpression?.Compile();
        var count = 0;
        await foreach (var item in _innerQueryable.IterateSourceAsync())
        {
            var collection = collectionSelector.Invoke(item);
            foreach (var child in collection)
            {
                var result = resultSelector.Invoke(item, child);
                if (condition?.Invoke(result) == false)
                {
                    continue;
                }
                count++;
                if (count <= _skip)
                {
                    continue;
                }
                yield return result;
                if (_take >= 0 && count >= _take)
                {
                    break;
                }
            }
        }
    }
}
