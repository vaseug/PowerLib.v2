using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Generic.Extensions;

public static class EqualityComparerExtension
{
  #region IEqualityComparer<T>

  public static bool Equals<T>(this IEqualityComparer<T> equalityComparer, T? xValue, T? yValue, bool nullVarious)
    where T : class
  {
    Argument.That.NotNull(equalityComparer);

    return xValue is not null && yValue is not null && equalityComparer.Equals(xValue, yValue) || xValue is null && yValue is null && !nullVarious;
  }

  public static bool Equals<T>(this IEqualityComparer<T> equalityComparer, T? xValue, T? yValue, bool nullVarious)
    where T : struct
  {
    Argument.That.NotNull(equalityComparer);

    return xValue is not null && yValue is not null && equalityComparer.Equals(xValue.Value, yValue.Value) || xValue is null && yValue is null && !nullVarious;
  }

  public static Equality<T?> AsEquality<T>(this IEqualityComparer<T> equalityComparer)
    => Argument.That.NotNull(equalityComparer).Equals;

  public static Equality<T> AsNonNullEquality<T>(this IEqualityComparer<T> equalityComparer)
    => Argument.That.NotNull(equalityComparer).Equals;

  public static Equality<T> AsNonNullableEquality<T>(this IEqualityComparer<T?> equalityComparer)
    where T : struct
    => (x, y) => Argument.That.NotNull(equalityComparer).Equals(x, y);

  public static Hasher<T> AsHasher<T>(this IEqualityComparer<T> equalityComparer)
    => ([DisallowNull] value) => Argument.That.NotNull(equalityComparer).GetHashCode(value);

  public static Hasher<T> AsNonNullableHasher<T>(this IEqualityComparer<T?> equalityComparer)
    where T : struct
    => value => Argument.That.NotNull(equalityComparer).GetHashCode(value);

  #endregion
}
