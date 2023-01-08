using System;
using System.Collections;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class SelectComparer : IComparer
{
  private readonly Converter<object?, object?> _selector;
  private readonly Comparison<object?>? _comparison;

  #region Constructors

  public SelectComparer(Converter<object?, object?> selector)
    : this(selector, default(Comparison<object?>))
  { }

  public SelectComparer(Converter<object?, object?> selector, Comparison<object?>? comparison)
  {
    _selector = Argument.That.NotNull(selector);
    _comparison = comparison;
  }

  public SelectComparer(Converter<object?, object?> selector, IComparer? comparer)
    : this(selector, comparer?.AsComparison())
  { }

  public SelectComparer(IConverter selector)
    : this(Argument.That.NotNull(selector).AsConverter(), default(Comparison<object?>))
  { }

  public SelectComparer(IConverter selector, Comparison<object?>? comparison)
    : this(Argument.That.NotNull(selector).AsConverter(), comparison)
  { }

  public SelectComparer(IConverter selector, IComparer comparer)
    : this(Argument.That.NotNull(selector).AsConverter(), comparer?.AsComparison())
  { }

  #endregion
  #region Methods

  public int Compare(object? x, object? y)
    => (_comparison ?? Comparer.Default.Compare)(_selector(x), _selector(y));

  #endregion
}
