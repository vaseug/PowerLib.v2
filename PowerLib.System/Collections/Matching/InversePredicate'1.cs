using System;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class InversePredicate<T> : IPredicate<T>, IPredicate
{
  private readonly Predicate<T?> _predicate;

  #region Constructors

  public InversePredicate(Predicate<T?> predicate)
  {
    _predicate = Argument.That.NotNull(predicate);
  }

  public InversePredicate(IPredicate<T> predicate)
  {
    _predicate = Argument.That.NotNull(predicate).AsPredicate();
  }

  #endregion
  #region Methods

  public bool Match(T? obj)
    => !_predicate(obj);

  #endregion
  #region Interfaces implementations

  bool IPredicate.Match(object? obj)
    => Match(Argument.That.OfType<T>(obj));

  #endregion
}
