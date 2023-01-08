using System;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class ComparisonPredicate<T> : IPredicate<T>, IPredicate
{
  private readonly Comparison<T?> _comparison;

  #region Constructors

  public ComparisonPredicate(T? value, Comparison<T?> comparison, ComparisonCriteria criteria)
  {
    _comparison = Argument.That.NotNull(comparison);
    Value = value;
    Criteria = criteria;
  }

  public ComparisonPredicate(T? value, IComparer<T> comparer, ComparisonCriteria criteria)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
    Value = value;
    Criteria = criteria;
  }

  #endregion
  #region Properties

  public T? Value { get; }

  public ComparisonCriteria Criteria { get; }

  #endregion
  #region Methods

  public bool Match(T? obj)
    => Comparison.Match(_comparison(obj, Value), Criteria);

  #endregion
  #region Interfaces implementations

  bool IPredicate.Match(object? obj)
    => Match(Argument.That.OfType<T>(obj));

  #endregion
}
