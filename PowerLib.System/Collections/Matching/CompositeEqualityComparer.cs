using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class CompositeEqualityComparer : IEqualityComparer
{
  private readonly Equality<object?>[] _equalities;
  private readonly Hasher<object>[]? _hashers;
  private readonly Func<int, int, int>? _composer;
  private readonly int _seed;

  #region Constructors

  public CompositeEqualityComparer(IEnumerable<Equality<object?>> equalities, Hasher<object>? hasher = default)
  {
    _equalities = Argument.That.NotEmpty(equalities)//.NonNullElements().Value
      .ToArray();
    _hashers = hasher is null ? null : new[] { hasher };
    _composer = default;
    _seed = 0;
  }

  public CompositeEqualityComparer(IEnumerable<(Equality<object?> equality, Hasher<object> hasher)> pairs, Func<int, int, int>? composer = default, int seed = 23)
  {
    var array = Argument.That.NotEmpty(pairs)//.MatchElements((item, index) => item.equality is mot null && item.hasher is mot null).Value
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

  public CompositeEqualityComparer(IEnumerable<IEqualityComparer> equalityComparers, Func<int, int, int>? composer = default, int seed = 23)
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

  public CompositeEqualityComparer(params Equality<object?>[] equalities)
    : this((IEnumerable<Equality<object?>>)equalities)
  { }

  public CompositeEqualityComparer(params IEqualityComparer[] equalityComparers)
    : this((IEnumerable<IEqualityComparer>)equalityComparers)
  { }

  #endregion
  #region Methods

  private static int ComposeHashCode(int accum, int hashCode)
    => unchecked(accum * 31 + hashCode);

  public new bool Equals(object? x, object? y)
  {
    bool result = true;
    for (int i = 0; result && i < _equalities.Length; i++)
      result = _equalities[i](x, y);
    return result;
  }

  public int GetHashCode(object obj)
    => obj is null ? 0 : _hashers?.Aggregate(_seed, (accum, hasher) => (_composer ?? ComposeHashCode)(accum, hasher(obj))) ?? EqualityComparer<object>.Default.GetHashCode(obj);

  #endregion
}
