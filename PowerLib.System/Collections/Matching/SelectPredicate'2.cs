using System;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class SelectPredicate<TSource, TInner> : IPredicate<TSource>, IPredicate
{
  private readonly Converter<TSource?, TInner?> _selector;
  private readonly Predicate<TInner?> _predicate;

  #region Constructors

  public SelectPredicate(Converter<TSource?, TInner?> selector, Predicate<TInner?> predicate)
  {
    _selector = Argument.That.NotNull(selector);
    _predicate = Argument.That.NotNull(predicate);
  }

  public SelectPredicate(Converter<TSource?, TInner?> selector, IPredicate<TInner> predicate)
    : this(selector, Argument.That.NotNull(predicate).AsPredicate())
  { }

  public SelectPredicate(IConverter<TSource, TInner> selector, Predicate<TInner?> predicate)
    : this(Argument.That.NotNull(selector).AsConverter(), predicate)
  { }

  public SelectPredicate(IConverter<TSource, TInner> selector, IPredicate<TInner> predicate)
    : this(Argument.That.NotNull(selector).AsConverter(), Argument.That.NotNull(predicate).AsPredicate())
  { }

  #endregion
  #region Methods

  public bool Match(TSource? obj)
    => _predicate(_selector(obj));

  #endregion
  #region Interfaces implementations

  bool IPredicate.Match(object? obj)
    => Match(Argument.That.OfType<TSource>(obj));

  #endregion
}
