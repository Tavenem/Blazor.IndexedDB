using System.Linq.Expressions;

namespace Tavenem.Blazor.IndexedDB;

internal class ReplaceExpressionVisitor(Expression oldValue, Expression newValue) : ExpressionVisitor
{
    /// <inheritdoc />
    public override Expression? Visit(Expression? node) => node == oldValue
        ? newValue
        : base.Visit(node);
}
