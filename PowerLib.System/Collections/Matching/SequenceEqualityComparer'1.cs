using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class SequenceEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>, IEqualityComparer
{
  private readonly Equality<T?>? _equality;
  private readonly Hasher<T>? _hasher;
  private readonly Func<int, int, int>? _composer;
  private readonly int _seed;

  private static readonly Lazy<SequenceEqualityComparer<T>> instance = new(() => new SequenceEqualityComparer<T>(default(IEqualityComparer<T>)));

  #region Constructors

  public SequenceEqualityComparer(Func<int, int, int>? composer = null, int seed = 23)
    : this(null, null, composer, seed)
  { }

  public SequenceEqualityComparer(IEqualityComparer<T>? equalityComparer, Func<int, int, int>? composer = null, int seed = 23)
    : this(equalityComparer?.AsEquality(), equalityComparer?.AsHasher(), composer, seed)
  { }

  public SequenceEqualityComparer(Equality<T?>? equality, Hasher<T>? hasher, Func<int, int, int>? composer = null, int seed = 23)
  {
    _equality = equality;
    _hasher = hasher;
    _composer = composer;
    _seed = seed;
  }

  #endregion
  #region Properties

  public static SequenceEqualityComparer<T> Default
    => instance.Value;

  #endregion
  #region Methods

  private static int ComposeHashCode(int accum, int hashCode)
    => accum * 31 + hashCode;

  private int ComposeHashCode(int accum, T item)
    => (_composer ?? ComposeHashCode)(accum, item is null ? 0 : (_hasher ?? EqualityComparer<T>.Default.GetHashCode)(item));

  public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
    => x is not null ? y is not null ? x.SequenceEqual(y, _equality ?? EqualityComparer<T>.Default.AsEquality<T>()) : false : y is null;

  public int GetHashCode([DisallowNull] IEnumerable<T> obj)
    => obj is null ? 0 : obj.Aggregate(_seed, ComposeHashCode);

  #endregion
  #region Interfaces implementation

  bool IEqualityComparer.Equals(object? x, object? y)
    => Equals(Argument.That.OfType<IEnumerable<T>>(x), Argument.That.OfType<IEnumerable<T>>(y));

  int IEqualityComparer.GetHashCode(object obj)
    => GetHashCode(Argument.That.OfType<IEnumerable<T>>(obj)!);

  #endregion
}
