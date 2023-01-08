using System.Collections;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.NonGeneric.Extensions;

public static class EqualityComparerExtension
{
  #region IEqualityComparer

  public static bool Equals(this IEqualityComparer equalityComparer, object? xValue, object? yValue, bool nullVarious)
  {
    Argument.That.NotNull(equalityComparer);

    return xValue is not null && yValue is not null && equalityComparer.Equals(xValue, yValue) || xValue is null && yValue is null && !nullVarious;
  }

  public static Equality<object?> AsEquality(this IEqualityComparer equalityComparer)
    => Argument.That.NotNull(equalityComparer).Equals;

  public static Hasher<object> AsHasher(this IEqualityComparer equalityComparer)
    => Argument.That.NotNull(equalityComparer).GetHashCode;

  #endregion
}
