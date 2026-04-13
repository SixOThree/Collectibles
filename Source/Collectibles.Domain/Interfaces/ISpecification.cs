using System.Linq.Expressions;

namespace Collectibles.Domain.Interfaces;

/// <summary>
/// Specification pattern interface for creating query specifications.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<T, object>> OrderBy { get; }
    Expression<Func<T, object>> OrderByDescending { get; }
    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }
}
