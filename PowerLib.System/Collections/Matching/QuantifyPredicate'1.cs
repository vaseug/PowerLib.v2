using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class QuantifyPredicate<T> : IPredicate<IEnumerable<T?>>, IPredicate
{
  private readonly Predicate<T?> _predicate;

  #region Constructors

  public QuantifyPredicate(Predicate<T?> predicate, QuantifyCriteria criteria)
  {
    _predicate = Argument.That.NotNull(predicate);
    Criteria = criteria;
  }

  public QuantifyPredicate(IPredicate<T> predicate, QuantifyCriteria criteria)
    : this(Argument.That.NotNull(predicate).AsPredicate(), criteria)
  { }

  #endregion
  #region Properties

  public QuantifyCriteria Criteria { get; }

  #endregion
  #region Methods

  public bool Match([NotNull] IEnumerable<T?>? obj)
  {
    Argument.That.NotNull(obj);

    using var e = obj.GetEnumerator();
    bool result = Criteria == QuantifyCriteria.All;
    while ((!result && Criteria == QuantifyCriteria.Any || result && Criteria == QuantifyCriteria.All) && e.MoveNext())
      result = _predicate(e.Current);
    return result;
  }

  #endregion
  #region Interfaces implementations

  bool IPredicate.Match(object? obj)
    => Match(Argument.That.OfType<IEnumerable<T>>(obj));

  #endregion
}
