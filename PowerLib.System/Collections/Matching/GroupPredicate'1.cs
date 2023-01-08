using System;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class GroupPredicate<T> : IPredicate<T>, IPredicate
{
  private readonly Predicate<T?>[] _predicates;

  #region Constructors

  public GroupPredicate(IEnumerable<Predicate<T?>> predicates, GroupCriteria criteria)
  {
    _predicates = Argument.That.NonNullElements(predicates)
      .ToArray();
    Criteria = criteria;
  }

  public GroupPredicate(IEnumerable<IPredicate<T>> predicates, GroupCriteria criteria)
  {
    _predicates = Argument.That.NonNullElements(predicates)
      .Select(predicate => predicate.AsPredicate())
      .ToArray();
    Criteria = criteria;
  }

  #endregion
  #region Properties

  public GroupCriteria Criteria { get; }

  #endregion
  #region Methods

  public bool Match(T? obj)
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
  #region Interfaces implementations

  bool IPredicate.Match(object? obj)
    => Match(Argument.That.OfType<T>(obj));

  #endregion
}
