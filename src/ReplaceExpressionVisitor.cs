using System.Linq.Expressions;

namespace Tavenem.Blazor.IndexedDB;

internal class ReplaceExpressionVisitor : ExpressionVisitor
{
    private readonly Expression _newValue;
    private readonly Expression _oldValue;

    public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
    {
        _oldValue = oldValue;
        _newValue = newValue;
    }

    /// <inheritdoc />
    public override Expression? Visit(Expression? node) => node == _oldValue
        ? _newValue
        : base.Visit(node);
}
