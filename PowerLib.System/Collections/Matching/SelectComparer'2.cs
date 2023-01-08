using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class SelectComparer<TSource, TInner> : IComparer<TSource>, IComparer
{
  private readonly Converter<TSource?, TInner?> _selector;
  private readonly Comparison<TInner?>? _comparison;

  #region Constructors

  public SelectComparer(Converter<TSource?, TInner?> selector)
    : this(selector, default(Comparison<TInner?>))
  { }

  public SelectComparer(Converter<TSource?, TInner?> selector, Comparison<TInner?>? comparison)
  {
    _selector = Argument.That.NotNull(selector);
    _comparison = comparison;
  }

  public SelectComparer(Converter<TSource?, TInner?> selector, IComparer<TInner>? comparer)
    : this(selector, comparer?.AsComparison())
  { }

  public SelectComparer(IConverter<TSource, TInner> selector)
    : this(Argument.That.NotNull(selector).AsConverter(), default(Comparison<TInner?>))
  { }

  public SelectComparer(IConverter<TSource, TInner> selector, Comparison<TInner?>? comparison)
    : this(Argument.That.NotNull(selector).AsConverter(), comparison)
  { }

  public SelectComparer(IConverter<TSource, TInner> selector, IComparer<TInner>? comparer)
    : this(Argument.That.NotNull(selector).AsConverter(), comparer?.AsComparison())
  { }

  #endregion
  #region Methods

  public int Compare(TSource? x, TSource? y)
    => (_comparison ?? Comparer<TInner>.Default.AsComparison<TInner>())(_selector(x), _selector(y));

  #endregion
  #region Interfaces implementation

  int IComparer.Compare(object? xValue, object? yValue)
    => Compare(Argument.That.OfType<TSource>(xValue), Argument.That.OfType<TSource>(yValue));

  #endregion
}
