using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class CompositeComparer : IComparer
{
  private readonly Comparison<object?>[] _comparisons;

  #region Constructors

  public CompositeComparer(IEnumerable<Comparison<object?>> comparisons)
  {
    _comparisons = Argument.That.NotEmpty(comparisons)//.NotNullElements().Value
      .ToArray();
  }

  public CompositeComparer(IEnumerable<IComparer> comparers)
  {
    _comparisons = Argument.That.NotEmpty(comparers)//.NotNullElements().Value
      .Select(comparer => comparer.AsComparison())
      .ToArray();
  }

  public CompositeComparer(params Comparison<object?>[] comparisons)
    : this((IEnumerable<Comparison<object?>>)comparisons)
  { }

  public CompositeComparer(params IComparer[] comparers)
    : this((IEnumerable<IComparer>)comparers)
  { }

  #endregion
  #region Methods

  public int Compare(object? x, object? y)
  {
    int result = 0;
    for (int i = 0; result == 0 && i < _comparisons.Length; i++)
      result = _comparisons[i](x, y);
    return result;
  }

  #endregion
}
