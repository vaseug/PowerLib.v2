using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class SequenceEqualityComparer : IEqualityComparer<IEnumerable>, IEqualityComparer
{
  private readonly Equality<object?>? _equality;
  private readonly Hasher<object>? _hasher;
  private readonly Func<int, int, int>? _composer;
  private readonly int _seed;

  private static readonly Lazy<SequenceEqualityComparer> instance = new(() => new SequenceEqualityComparer(default(IEqualityComparer)));

  #region Constructors

  public SequenceEqualityComparer(Func<int, int, int>? composer = null, int seed = 23)
    : this(null, null, composer, seed)
  { }

  public SequenceEqualityComparer(IEqualityComparer? equalityComparer, Func<int, int, int>? composer = null, int seed = 23)
    : this(equalityComparer?.AsEquality(), equalityComparer?.AsHasher(), composer, seed)
  { }

  public SequenceEqualityComparer(Equality<object?>? equality, Hasher<object>? hasher, Func<int, int, int>? composer = null, int seed = 23)
  {
    _equality = equality;
    _hasher = hasher;
    _composer = composer;
    _seed = seed;
  }

  #endregion
  #region Properties

  public static SequenceEqualityComparer Default
    => instance.Value;

  #endregion
  #region Methods

  private static int ComposeHashCode(int accum, int hashCode)
    => accum * 31 + hashCode;

  private int ComposeHashCode(int accum, object item)
    => (_composer ?? ComposeHashCode)(accum, item is null ? 0 : (_hasher ?? EqualityComparer<object>.Default.GetHashCode)(item));

  public bool Equals(IEnumerable? x, IEnumerable? y)
    => x is not null ? y is not null && x.OfType<object?>().SequenceEqual(y.OfType<object?>(), _equality ?? EqualityComparer<object>.Default.AsEquality<object>()) : y is null;

  public int GetHashCode(IEnumerable obj)
    => Argument.That.NotNull(obj).Cast<object>().Aggregate(_seed, ComposeHashCode);

  #endregion
  #region Interfaces implementation

  bool IEqualityComparer.Equals(object? x, object? y)
    => Equals(Argument.That.OfType<IEnumerable>(x), Argument.That.OfType<IEnumerable>(y));

  int IEqualityComparer.GetHashCode(object obj)
    => GetHashCode(Argument.That.OfType<IEnumerable>(obj)!);

  #endregion
}
