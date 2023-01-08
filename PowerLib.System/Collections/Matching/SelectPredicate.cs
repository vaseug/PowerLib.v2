using System;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class SelectPredicate : IPredicate
{
  private readonly Converter<object?, object?> _selector;
  private readonly Predicate<object?> _predicate;

  #region Constructors

  public SelectPredicate(Converter<object?, object?> selector, Predicate<object?> predicate)
  {
    _selector = Argument.That.NotNull(selector);
    _predicate = Argument.That.NotNull(predicate);
  }

  public SelectPredicate(Converter<object?, object?> selector, IPredicate predicate)
    : this(selector, Argument.That.NotNull(predicate).AsPredicate())
  { }

  public SelectPredicate(IConverter selector, Predicate<object?> predicate)
    : this(Argument.That.NotNull(selector).AsConverter(), predicate)
  { }

  public SelectPredicate(IConverter selector, IPredicate predicate)
    : this(Argument.That.NotNull(selector).AsConverter(), Argument.That.NotNull(predicate).AsPredicate())
  { }

  #endregion
  #region Methods

  public bool Match(object? obj)
    => _predicate(_selector(obj));

  #endregion
}
