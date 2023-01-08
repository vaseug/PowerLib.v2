using System;
using System.Collections;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class BetweenPredicate : IPredicate
{
  private readonly Comparison<object?> _comparison;

  #region Constructors

  public BetweenPredicate(object? lowerBound, object? upperBound, Comparison<object?> comparison, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
  {
    _comparison = Argument.That.NotNull(comparison);
    LowerBound = lowerBound;
    UpperBound = upperBound;
    Criteria = criteria;
  }

  public BetweenPredicate(object? lowerBound, object? upperBound, IComparer comparer, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
  {
    _comparison = Argument.That.NotNull(comparer).Compare;
    LowerBound = lowerBound;
    UpperBound = upperBound;
    Criteria = criteria;
  }

  #endregion
  #region Properties

  public object? LowerBound { get; }

  public object? UpperBound { get; }

  public BetweenCriteria Criteria { get; }

  #endregion
  #region Methods

  public bool Match(object? obj)
    => Comparison.Match(_comparison(obj, LowerBound), _comparison(obj, UpperBound), Criteria);

  #endregion
}
