using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class CustomComparer<T> : IComparer<T>, IComparer
{
  private readonly Comparison<T?> _comparison;

  #region Constructor

  public CustomComparer(Comparison<T?> comparison)
  {
    _comparison = Argument.That.NotNull(comparison);
  }

  public CustomComparer(IComparer<T> comparer)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
  }

  #endregion
  #region Methods

  public int Compare(T? x, T? y)
    => _comparison(x, y);

  #endregion
  #region Interfaces implementations

  int IComparer.Compare(object? x, object? y)
    => Compare(Argument.That.OfType<T>(x), Argument.That.OfType<T>(y));

  #endregion
}
