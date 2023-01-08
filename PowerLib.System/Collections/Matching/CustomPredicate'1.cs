using System;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class CustomPredicate<T> : IPredicate<T>
{
  private readonly Predicate<T?> _predicate;

  #region Constructors

  public CustomPredicate(Predicate<T?> predicate)
  {
    _predicate = Argument.That.NotNull(predicate);
  }

  public CustomPredicate(IPredicate<T> predicate)
  {
    _predicate = Argument.That.NotNull(predicate).AsPredicate();
  }

  #endregion
  #region Methods

  public bool Match(T? obj)
    => _predicate(obj);

  #endregion
}
