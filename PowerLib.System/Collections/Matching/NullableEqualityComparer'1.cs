using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching
{
  public sealed class NullableEqualityComparer<T> : IEqualityComparer<T?>, IEqualityComparer
    where T : struct
  {
    private readonly Equality<T>? _equality;
    private readonly Hasher<T>? _hasher;

    private static readonly Lazy<NullableEqualityComparer<T>> instance = new(() => new NullableEqualityComparer<T>(false));
    private static readonly Lazy<NullableEqualityComparer<T>> instanceNullVarious = new(() => new NullableEqualityComparer<T>(true));

    #region Constructors

    private NullableEqualityComparer(bool nullVarious)
    {
      _equality = default;
      _hasher = default;
      NullVarious = nullVarious;
    }

    public NullableEqualityComparer(Equality<T> equality, bool nullVarious)
    {
      _equality = Argument.That.NotNull(equality);
      _hasher = default;
      NullVarious = nullVarious;
    }

    public NullableEqualityComparer(Equality<T> equality, Hasher<T> hasher, bool nullVarious)
    {
      _equality = Argument.That.NotNull(equality);
      _hasher = Argument.That.NotNull(hasher);
      NullVarious = nullVarious;
    }

    public NullableEqualityComparer(IEqualityComparer<T> equalityComparer, bool nullInequal)
    {
      Argument.That.NotNull(equalityComparer);

      _equality = equalityComparer.Equals;
      _hasher = equalityComparer.GetHashCode;
      NullVarious = nullInequal;
    }

    #endregion
    #region Properties

    public static NullableEqualityComparer<T> Default
      => instance.Value;

    public static NullableEqualityComparer<T> DefaultNullVarious
      => instanceNullVarious.Value;

    public bool NullVarious { get; }

    #endregion
    #region Methods

    public bool Equals(T? x, T? y)
      => x is not null ? y is not null ? (_equality ?? EqualityComparer<T>.Default.Equals)(x.Value, y.Value) : false : y is not null ? false : !NullVarious;

    public int GetHashCode(T? obj)
      => obj is null ? 0 : (_hasher ?? EqualityComparer<T>.Default.GetHashCode)(obj.Value);

    #endregion
    #region Interfaces implementations

    bool IEqualityComparer.Equals(object? x, object? y)
      => Equals(Argument.That.OfType<T>(x), Argument.That.OfType<T>(y));

    int IEqualityComparer.GetHashCode(object value)
      => GetHashCode(Argument.That.OfType<T>(value));

    #endregion
  }
}
