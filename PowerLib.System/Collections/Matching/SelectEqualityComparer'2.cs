using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class SelectEqualityComparer<TSource, TInner> : IEqualityComparer<TSource>, IEqualityComparer
{
  private readonly Converter<TSource?, TInner?> _selector;
  private readonly Equality<TInner?>? _equality;
  private readonly Hasher<TInner>? _hasher;

  #region Constructors

  public SelectEqualityComparer(Converter<TSource?, TInner?> selector)
    : this(selector, default, default)
  { }

  public SelectEqualityComparer(Converter<TSource?, TInner?> selector, Equality<TInner?>? equality, Hasher<TInner>? hasher)
  {
    _selector = Argument.That.NotNull(selector);
    _equality = equality;
    _hasher = hasher;
  }

  public SelectEqualityComparer(Converter<TSource?, TInner?> selector, IEqualityComparer<TInner>? equalityComparer)
    : this(selector, equalityComparer?.AsEquality(), equalityComparer?.AsHasher())
  { }

  public SelectEqualityComparer(IConverter<TSource, TInner> selector)
    : this(Argument.That.NotNull(selector).AsConverter(), default, default)
  { }

  public SelectEqualityComparer(IConverter<TSource, TInner> selector, Equality<TInner?>? equality, Hasher<TInner>? hasher)
    : this(Argument.That.NotNull(selector).AsConverter(), equality, hasher)
  { }

  public SelectEqualityComparer(IConverter<TSource, TInner> selector, IEqualityComparer<TInner>? equalityComparer)
    : this(Argument.That.NotNull(selector).AsConverter(), equalityComparer?.AsEquality(), equalityComparer?.AsHasher())
  { }

  #endregion
  #region Methods

  public bool Equals(TSource? x, TSource? y)
    => (_equality ?? EqualityComparer<TInner>.Default.AsEquality())(_selector(x), _selector(y));

  public int GetHashCode(TSource obj)
  {
    var inner = _selector(obj);
    return inner is null ? 0 : (_hasher ?? EqualityComparer<TInner>.Default.GetHashCode)(inner);
  }

  #endregion
  #region Interfaces implementations

  bool IEqualityComparer.Equals(object? x, object? y)
    => Equals(Argument.That.OfType<TSource>(x), Argument.That.OfType<TSource>(y));

  int IEqualityComparer.GetHashCode(object obj)
    => GetHashCode(Argument.That.OfType<TSource>(obj)!);

  #endregion
}
