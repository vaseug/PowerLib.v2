using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class QuantifyPredicate : IPredicate<IEnumerable>, IPredicate
{
  private readonly Predicate<object> _predicate;

  #region Constructors

  public QuantifyPredicate(Predicate<object> predicate, QuantifyCriteria criteria)
  {
    _predicate = Argument.That.NotNull(predicate);
    Criteria = criteria;
  }

  public QuantifyPredicate(IPredicate predicate, QuantifyCriteria criteria)
    : this(Argument.That.NotNull(predicate).AsPredicate(), criteria)
  { }

  #endregion
  #region Properties

  public QuantifyCriteria Criteria { get; }

  #endregion
  #region Methods

  public bool Match([NotNull] IEnumerable? obj)
  {
    Argument.That.NotNull(obj);

    var e = obj.GetEnumerator();
    using var d = e as IDisposable;
    bool result = Criteria == QuantifyCriteria.All;
    while ((!result && Criteria == QuantifyCriteria.Any || result && Criteria == QuantifyCriteria.All) && e.MoveNext())
      result = _predicate(e.Current);
    return result;
  }

  #endregion
  #region Interfaces implementations

  bool IPredicate.Match(object? obj)
    => Match(Argument.That.OfType<IEnumerable>(obj));

  #endregion
}
