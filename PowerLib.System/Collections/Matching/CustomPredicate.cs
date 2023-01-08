using System;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class CustomPredicate : IPredicate
{
  private readonly Predicate<object?> _predicate;

  #region Constructors

  public CustomPredicate(Predicate<object?> predicate)
  {
    _predicate = Argument.That.NotNull(predicate);
  }

  public CustomPredicate(IPredicate predicate)
  {
    _predicate = Argument.That.NotNull(predicate).Match;
  }

  #endregion
  #region Methods

  public bool Match(object? obj)
    => _predicate(obj);

  #endregion
}
