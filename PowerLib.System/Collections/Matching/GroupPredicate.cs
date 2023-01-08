using System;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class GroupPredicate : IPredicate
{
  private readonly Predicate<object?>[] _predicates;

  #region Constructors

  public GroupPredicate(IEnumerable<Predicate<object?>> predicates, GroupCriteria criteria)
  {
    _predicates = Argument.That.NotEmpty(predicates)//.NonNullElements().Value
      .ToArray();
    Criteria = criteria;
  }

  public GroupPredicate(IEnumerable<IPredicate> predicates, GroupCriteria criteria)
  {
    _predicates = Argument.That.NotEmpty(predicates)//.NonNullElements().Value
      .Select(predicate => predicate.AsPredicate())
      .ToArray();
    Criteria = criteria;
  }

  #endregion
  #region Properties

  public GroupCriteria Criteria { get; }

  #endregion
  #region Methods

  public bool Match(object? obj)
  {
    switch (Criteria)
    {
      case GroupCriteria.And:
        for (int i = 0; i < _predicates.Length; i++)
          if (!_predicates[i](obj))
            return false;
        return true;
      case GroupCriteria.Or:
        for (int i = 0; i < _predicates.Length; i++)
          if (_predicates[i](obj))
            return true;
        return false;
      default:
        return Operation.That.Failed<bool>();
    }
  }

  #endregion
}
