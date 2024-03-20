using System.Linq.Expressions;
using System.Text.Json.Serialization.Metadata;
using Tavenem.DataStorage;

namespace Tavenem.Blazor.IndexedDB;

/// <summary>
/// Provides LINQ operations on an <see cref="IndexedDbService"/>.
/// </summary>
internal sealed class SelectedIndexedDbQueryable<T, U> : IndexedDbQueryable<T>
{
    private readonly IndexedDbQueryable<U> _innerQueryable;
    private readonly Expression<Func<U, T>>? _selectExpression;
    private readonly Expression<Func<U, IEnumerable<T>>>? _selectManyExpression;

    /// <summary>
    /// Constructs a new instance of <see cref="SelectedIndexedDbQueryable{T, U}"/>.
    /// </summary>
    public SelectedIndexedDbQueryable(IndexedDbQueryable<U> inner, Expression<Func<U, T>>? expression, JsonTypeInfo<T>? typeInfo = null)
        : base(typeInfo)
    {
        _innerQueryable = inner;
        _selectExpression = expression;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="SelectedIndexedDbQueryable{T, U}"/>.
    /// </summary>
    public SelectedIndexedDbQueryable(IndexedDbQueryable<U> inner, Expression<Func<U, IEnumerable<T>>>? expression, JsonTypeInfo<T>? typeInfo = null)
        : base(typeInfo)
    {
        _innerQueryable = inner;
        _selectManyExpression = expression;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="SelectedIndexedDbQueryable{T, U}"/>.
    /// </summary>
    private SelectedIndexedDbQueryable(
        IndexedDbQueryable<U> inner,
        Expression<Func<U, T>>? selectExpression,
        Expression<Func<U, IEnumerable<T>>>? selectManyExpression,
        Expression<Func<T, bool>>? conditionalExpression,
        int skip,
        int take,
        JsonTypeInfo<T>? typeInfo = null)
        : base(conditionalExpression, skip, take, typeInfo)
    {
        _innerQueryable = inner;
        _selectExpression = selectExpression;
        _selectManyExpression = selectManyExpression;
    }

    /// <inheritdoc/>
    public override IDataStoreQueryable<T> Skip(int count) => new SelectedIndexedDbQueryable<T, U>(
        _innerQueryable,
        _selectExpression,
        _selectManyExpression,
        _conditionalExpression,
        count,
        _take);

    /// <inheritdoc/>
    public override IDataStoreQueryable<T> Take(int count) => new SelectedIndexedDbQueryable<T, U>(
        _innerQueryable,
        _selectExpression,
        _selectManyExpression,
        _conditionalExpression,
        _skip,
        count);

    /// <inheritdoc/>
    public override IDataStoreQueryable<T> Where(Expression<Func<T, bool>> predicate) => new SelectedIndexedDbQueryable<T, U>(
        _innerQueryable,
        _selectExpression,
        _selectManyExpression,
        _conditionalExpression is null
            ? predicate
            : CombineCondition(predicate),
        _skip,
        _take);

    internal override IEnumerable<T> IterateSource() => _selectManyExpression is not null
        ? IterateSourceMultiple()
        : IterateSourceSingle();

    internal override IAsyncEnumerable<T> IterateSourceAsync() => _selectManyExpression is not null
        ? IterateSourceMultipleAsync()
        : IterateSourceSingleAsync();

    private IEnumerable<T> IterateSourceSingle()
    {
        var select = _selectExpression?.Compile();
        if (select is null)
        {
            yield break;
        }
        var condition = _conditionalExpression?.Compile();
        var count = 0;
        foreach (var item in _innerQueryable.IterateSource())
        {
            var selected = select.Invoke(item);
            if (condition?.Invoke(selected) == false)
            {
                continue;
            }
            count++;
            if (count <= _skip)
            {
                continue;
            }
            yield return selected;
            if (_take >= 0 && count >= _take)
            {
                break;
            }
        }
    }

    private async IAsyncEnumerable<T> IterateSourceSingleAsync()
    {
        var select = _selectExpression?.Compile();
        if (select is null)
        {
            yield break;
        }
        var condition = _conditionalExpression?.Compile();
        var count = 0;
        await foreach (var item in _innerQueryable.IterateSourceAsync())
        {
            var selected = select.Invoke(item);
            if (condition?.Invoke(selected) == false)
            {
                continue;
            }
            count++;
            if (count <= _skip)
            {
                continue;
            }
            yield return selected;
            if (_take >= 0 && count >= _take)
            {
                break;
            }
        }
    }

    private IEnumerable<T> IterateSourceMultiple()
    {
        var select = _selectManyExpression?.Compile();
        if (select is null)
        {
            yield break;
        }
        var condition = _conditionalExpression?.Compile();
        var count = 0;
        foreach (var item in _innerQueryable.IterateSource())
        {
            var collection = select.Invoke(item);
            foreach (var child in collection)
            {
                if (condition?.Invoke(child) == false)
                {
                    continue;
                }
                count++;
                if (count <= _skip)
                {
                    continue;
                }
                yield return child;
                if (_take >= 0 && count >= _take)
                {
                    break;
                }
            }
        }
    }

    private async IAsyncEnumerable<T> IterateSourceMultipleAsync()
    {
        var select = _selectManyExpression?.Compile();
        if (select is null)
        {
            yield break;
        }
        var condition = _conditionalExpression?.Compile();
        var count = 0;
        await foreach (var item in _innerQueryable.IterateSourceAsync())
        {
            var collection = select.Invoke(item);
            foreach (var child in collection)
            {
                if (condition?.Invoke(child) == false)
                {
                    continue;
                }
                count++;
                if (count <= _skip)
                {
                    continue;
                }
                yield return child;
                if (_take >= 0 && count >= _take)
                {
                    break;
                }
            }
        }
    }
}
