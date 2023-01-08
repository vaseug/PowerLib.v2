using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class CompositeEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
{
  private readonly Equality<T?>[] _equalities;
  private readonly Hasher<T>[]? _hashers;
  private readonly Func<int, int, int>? _composer;
  private readonly int _seed;

  #region Constructors

  public CompositeEqualityComparer(IEnumerable<Equality<T?>> equalities, Hasher<T>? hasher = default)
  {
    _equalities = Argument.That.NotEmptyNonNullElements(equalities).ToArray();
    _hashers = hasher is null ? null : new[] { hasher };
    _composer = default;
    _seed = 0;
  }

  public CompositeEqualityComparer(IEnumerable<(Equality<T?> equality, Hasher<T> hasher)> pairs, Func<int, int, int>? composer = default, int seed = 23)
  {
    var array = Argument.That.NotEmpty(pairs)//.MatchElements((item, index) => item.equality is not null && item.hasher is not null).Value
      .ToArray();
    _equalities = array
      .Select(pair => pair.equality)
      .ToArray();
    _hashers = pairs
      .Select(pair => pair.hasher)
      .ToArray();
    _composer = composer;
    _seed = seed;
  }

  public CompositeEqualityComparer(IEnumerable<IEqualityComparer<T>> equalityComparers, Func<int, int, int>? composer = default, int seed = 23)
  {
    var pairs = Argument.That.NotEmpty(equalityComparers)//.NonNullElements().Value
      .Select(equalityComparer => (equality: equalityComparer.AsEquality(), hasher: equalityComparer.AsHasher()))
      .ToArray();
    _equalities = pairs
      .Select(pair => pair.equality)
      .ToArray();
    _hashers = pairs
      .Select(pair => pair.hasher)
      .ToArray();
    _composer = composer;
    _seed = seed;
  }

  public CompositeEqualityComparer(params Equality<T?>[] equalities)
    : this((IEnumerable<Equality<T?>>)equalities)
  { }

  public CompositeEqualityComparer(params IEqualityComparer<T>[] equalityComparers)
    : this((IEnumerable<IEqualityComparer<T>>)equalityComparers)
  { }

  #endregion
  #region Methods

  private static int ComposeHashCode(int accum, int hashCode)
    => unchecked(accum * 31 + hashCode);

  public bool Equals(T? x, T? y)
  {
    bool result = true;
    for (int i = 0; result && i < _equalities.Length; i++)
      result = _equalities[i](x, y);
    return result;
  }

  public int GetHashCode(T obj)
    => obj is null ? 0 : _hashers?.Aggregate(_seed, (accum, hasher) => (_composer ?? ComposeHashCode)(accum, hasher(obj))) ?? EqualityComparer<T>.Default.GetHashCode(obj);

  #endregion
  #region Interfaces implementations

  bool IEqualityComparer.Equals(object? xValue, object? yValue)
    => Equals(Argument.That.OfType<T>(xValue), Argument.That.OfType<T>(yValue));

  int IEqualityComparer.GetHashCode(object value)
    => GetHashCode(Argument.That.OfType<T>(value)!);

  #endregion
}
