using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching
{
    public sealed class NonNullableEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
    where T : struct
  {
    private readonly Equality<T>? _equality;
    private readonly Hasher<T>? _hasher;

    #region Constructors

    public NonNullableEqualityComparer(Equality<T?> equality, bool nullVarious)
    {
      _equality = Argument.That.NotNull(equality).AsNonNullableEquality();
      _hasher = default;
    }

    public NonNullableEqualityComparer(Equality<T?> equality, Hasher<T?> hasher)
    {
      _equality = Argument.That.NotNull(equality).AsNonNullableEquality();
      _hasher = Argument.That.NotNull(hasher).AsNonNullableHasher();
    }

    public NonNullableEqualityComparer(IEqualityComparer<T?> equalityComparer)
    {
      Argument.That.NotNull(equalityComparer);

      _equality = Argument.That.NotNull(equalityComparer).AsNonNullableEquality();
      _hasher = Argument.That.NotNull(equalityComparer).AsNonNullableHasher();
    }

    #endregion
    #region Methods

    public bool Equals(T x, T y)
      => (_equality ?? EqualityComparer<T>.Default.Equals)(x, y);

    public int GetHashCode(T obj)
      => (_hasher ?? EqualityComparer<T>.Default.GetHashCode)(obj);

    #endregion
    #region Interfaces implementations

    bool IEqualityComparer.Equals(object? x, object? y)
      => Equals(Argument.That.OfType<T>(x), Argument.That.OfType<T>(y));

    int IEqualityComparer.GetHashCode(object value)
      => GetHashCode(Argument.That.OfType<T>(value));

    #endregion
  }
}
