using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class NonNullableComparer<T> : IComparer<T>, IComparer
  where T : struct
{
  private readonly Comparison<T>? _comparison;

  #region Constructors

  public NonNullableComparer(Comparison<T?> comparison)
  {
    _comparison = Argument.That.NotNull(comparison).AsNonNullableComparison();
  }

  public NonNullableComparer(IComparer<T?> comparer)
  {
    _comparison = Argument.That.NotNull(comparer).AsNonNullableComparison();
  }

  #endregion
  #region Methods

  public int Compare(T x, T y)
    => (_comparison ?? Comparer<T>.Default.Compare)(x, y);

  #endregion
  #region Interfaces implementations

  int IComparer.Compare(object? x, object? y)
    => Compare(Argument.That.OfType<T>(x), Argument.That.OfType<T>(y));

  #endregion
}
