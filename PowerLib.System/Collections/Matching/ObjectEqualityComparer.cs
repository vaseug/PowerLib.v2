using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching
{
    public sealed class ObjectEqualityComparer : IEqualityComparer
  {
    private readonly Equality<object>? _equality;
    private readonly Hasher<object>? _hasher;

    private static readonly Lazy<ObjectEqualityComparer> instance = new(() => new ObjectEqualityComparer(false));
    private static readonly Lazy<ObjectEqualityComparer> instanceNullVarious = new(() => new ObjectEqualityComparer(true));

    #region Constructors

    private ObjectEqualityComparer(bool nullVarious)
    {
      _equality = default;
      _hasher = default;
      NullVarious = nullVarious;
    }

    public ObjectEqualityComparer(Equality<object> equality, bool nullVarious)
    {
      _equality = Argument.That.NotNull(equality);
      _hasher = default;
      NullVarious = nullVarious;
    }

    public ObjectEqualityComparer(Equality<object> equality, Hasher<object> hasher, bool nullVarious)
    {
      _equality = Argument.That.NotNull(equality);
      _hasher = Argument.That.NotNull(hasher);
      NullVarious = nullVarious;
    }

    public ObjectEqualityComparer(IEqualityComparer? equalityComparer, bool nullVarious)
    {
      Argument.That.NotNull(equalityComparer);

      _equality = equalityComparer.AsEquality();
      _hasher = equalityComparer.AsHasher();
      NullVarious = nullVarious;
    }

    #endregion
    #region Properties

    public static ObjectEqualityComparer Default
      => instance.Value;

    public static ObjectEqualityComparer DefaultNullVarious
      => instanceNullVarious.Value;

    public bool NullVarious { get; }

    #endregion
    #region Methods

    public new bool Equals(object? x, object? y)
      => x is not null ? y is not null ? (_equality ?? EqualityComparer<object>.Default.Equals)(x, y) : false : y is not null ? false : !NullVarious;

    public int GetHashCode(object obj)
      => obj is null ? 0 : (_hasher ?? EqualityComparer<object>.Default.GetHashCode)(obj);

    #endregion
  }
}
