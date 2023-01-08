using System;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class BetweenPredicate<T> : IPredicate<T>, IPredicate
{
  private readonly Comparison<T?> _comparison;

  #region Constructors

  public BetweenPredicate(T? lowerBound, T? upperBound, Comparison<T?> comparison, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
  {
    _comparison = Argument.That.NotNull(comparison);
    LowerBound = lowerBound;
    UpperBound = upperBound;
    Criteria = criteria;
  }

  public BetweenPredicate(T? lowerBound, T? upperBound, IComparer<T> comparer, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
    LowerBound = lowerBound;
    UpperBound = upperBound;
    Criteria = criteria;
  }

  #endregion
  #region Properties

  public T? LowerBound { get; }

  public T? UpperBound { get; }

  public BetweenCriteria Criteria { get; }

  #endregion
  #region Methods

  public bool Match(T? obj)
    => Comparison.Match(_comparison(obj, LowerBound), _comparison(obj, UpperBound), Criteria);

  #endregion
  #region Interfaces implementations

  bool IPredicate.Match(object? obj)
    => Match(Argument.That.OfType<T>(obj));

  #endregion
}
