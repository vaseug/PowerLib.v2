using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching
{
    public sealed class ObjectEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
    where T : class
  {
    private readonly Equality<T>? _equality;
    private readonly Hasher<T>? _hasher;

    private static readonly Lazy<ObjectEqualityComparer<T>> instance = new(() => new ObjectEqualityComparer<T>(false));
    private static readonly Lazy<ObjectEqualityComparer<T>> instanceNullVarious = new(() => new ObjectEqualityComparer<T>(true));

    #region Constructors

    private ObjectEqualityComparer(bool nullVarious)
    {
      _equality = default;
      _hasher = default;
      NullVarious = nullVarious;
    }

    public ObjectEqualityComparer(Equality<T> equality, bool nullVarious)
    {
      _equality = Argument.That.NotNull(equality);
      _hasher = default;
      NullVarious = nullVarious;
    }

    public ObjectEqualityComparer(Equality<T> equality, Hasher<T> hasher, bool nullVarious)
    {
      _equality = Argument.That.NotNull(equality);
      _hasher = Argument.That.NotNull(hasher);
      NullVarious = nullVarious;
    }

    public ObjectEqualityComparer(IEqualityComparer<T> equalityComparer, bool nullVarious)
    {
      Argument.That.NotNull(equalityComparer);

      _equality = equalityComparer.AsEquality();
      _hasher = equalityComparer.AsHasher();
      NullVarious = nullVarious;
    }

    #endregion
    #region Properties

    public static ObjectEqualityComparer<T> Default
      => instance.Value;

    public static ObjectEqualityComparer<T> DefaultNullVarious
      => instanceNullVarious.Value;

    public bool NullVarious { get; }

    #endregion
    #region Methods

    public bool Equals(T? x, T? y)
      => x is not null ? y is not null ? (_equality ?? EqualityComparer<T>.Default.Equals)(x, y) : false : y is not null ? false : !NullVarious;

    public int GetHashCode(T obj)
      => obj is null ? 0 : (_hasher ?? EqualityComparer<T>.Default.GetHashCode)(obj);

    #endregion
    #region Interfaces implementations

    bool IEqualityComparer.Equals(object? x, object? y)
      => Equals(Argument.That.OfType<T>(x), Argument.That.OfType<T>(y));

    int IEqualityComparer.GetHashCode(object obj)
      => GetHashCode(Argument.That.OfType<T>(obj)!);

    #endregion
  }
}
