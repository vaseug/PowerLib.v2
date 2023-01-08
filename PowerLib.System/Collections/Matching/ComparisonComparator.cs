using System;
using System.Collections;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class ComparisonComparator : IComparator<object>, IComparator
{
  private readonly Comparison<object?> _comparison;

  #region Constructors

  public ComparisonComparator(object? value, Comparison<object?> comparison)
  {
    _comparison = Argument.That.NotNull(comparison);
    Value = value;
  }

  public ComparisonComparator(object? value, IComparer comparer)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
    Value = value;
  }

  #endregion
  #region Properties

  public object? Value { get; }

  #endregion
  #region Methods

  public int Compare(object? obj)
    => _comparison(Value, obj);

  #endregion
}
