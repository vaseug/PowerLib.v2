using System;
using System.Collections;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class ComparisonPredicate : IPredicate
{
  private readonly Comparison<object?> _comparison;

  #region Constructors

  public ComparisonPredicate(object? value, Comparison<object?> comparison, ComparisonCriteria criteria)
  {
    _comparison = Argument.That.NotNull(comparison);
    Value = value;
    Criteria = criteria;
  }

  public ComparisonPredicate(object? value, IComparer comparer, ComparisonCriteria criteria)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
    Value = value;
    Criteria = criteria;
  }

  #endregion
  #region Properties

  public object? Value { get; }

  public ComparisonCriteria Criteria { get; }

  #endregion
  #region Methods

  public bool Match(object? obj)
    => Comparison.Match(_comparison(obj, Value), Criteria);

  #endregion
}
