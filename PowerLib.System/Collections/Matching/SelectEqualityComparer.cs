using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class SelectEqualityComparer : IEqualityComparer
{
  private readonly Converter<object?, object?> _selector;
  private readonly Equality<object?>? _equality;
  private readonly Hasher<object>? _hasher;

  #region Constructors

  public SelectEqualityComparer(Converter<object?, object?> selector)
    : this(selector, default, default)
  { }

  public SelectEqualityComparer(Converter<object?, object?> selector, Equality<object?>? equality, Hasher<object>? hasher)
  {
    _selector = Argument.That.NotNull(selector);
    _equality = equality;
    _hasher = hasher;
  }

  public SelectEqualityComparer(Converter<object?, object?> selector, IEqualityComparer? equalityComparer)
    : this(selector, equalityComparer?.AsEquality(), equalityComparer?.AsHasher())
  { }

  public SelectEqualityComparer(IConverter selector)
    : this(Argument.That.NotNull(selector).AsConverter(), default, default)
  { }

  public SelectEqualityComparer(IConverter selector, Equality<object?>? equality, Hasher<object>? hasher)
    : this(Argument.That.NotNull(selector).AsConverter(), equality, hasher)
  { }

  public SelectEqualityComparer(IConverter selector, IEqualityComparer? equalityComparer)
    : this(Argument.That.NotNull(selector).AsConverter(), equalityComparer?.AsEquality(), equalityComparer?.AsHasher())
  { }

  #endregion
  #region Methods

  public new bool Equals(object? x, object? y)
    => (_equality ?? EqualityComparer<object>.Default.AsEquality())(_selector(x), _selector(y));

  public int GetHashCode(object obj)
  {
    var inner = _selector(obj);
    return inner is null ? 0 : (_hasher ?? EqualityComparer<object>.Default.GetHashCode)(inner);
  }

  #endregion
}
