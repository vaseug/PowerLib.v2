using System;
using System.Collections;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class CustomComparer : IComparer
{
  private readonly Comparison<object?> _comparison;

  #region Constructor

  public CustomComparer(Comparison<object?> comparison)
  {
    _comparison = Argument.That.NotNull(comparison);
  }

  public CustomComparer(IComparer comparer)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
  }

  #endregion
  #region Methods

  public int Compare(object? x, object? y)
    => _comparison(x, y);

  #endregion
}
