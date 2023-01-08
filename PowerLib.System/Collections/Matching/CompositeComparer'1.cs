using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class CompositeComparer<T> : IComparer<T>, IComparer
{
  private readonly Comparison<T?>[] _comparisons;

  #region Constructors

  public CompositeComparer(IEnumerable<Comparison<T?>> comparisons)
  {
    _comparisons = Argument.That.NotEmpty(comparisons)//.NotNullElements().Value
      .ToArray();
  }

  public CompositeComparer(IEnumerable<IComparer<T>> comparers)
  {
    _comparisons = Argument.That.NotEmpty(comparers)//.NotNullElements().Value
      .Select(comparer => comparer.AsComparison())
      .ToArray();
  }

  public CompositeComparer(params Comparison<T?>[] comparisons)
    : this((IEnumerable<Comparison<T?>>)comparisons)
  { }

  public CompositeComparer(params IComparer<T>[] comparers)
    : this((IEnumerable<IComparer<T>>)comparers)
  { }

  #endregion
  #region Methods

  public int Compare(T? x, T? y)
  {
    int result = 0;
    for (int i = 0; result == 0 && i < _comparisons.Length; i++)
      result = _comparisons[i](x, y);
    return result;
  }

  #endregion
  #region Interfaces implementations

  int IComparer.Compare(object? x, object? y)
    => Compare(Argument.That.OfType<T>(x), Argument.That.OfType<T>(y));

  #endregion
}
