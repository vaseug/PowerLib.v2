using System;
using System.Collections;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class InverseComparer : IComparer
{
  private readonly Comparison<object?>? _comparison;

  #region Constructors

  public InverseComparer()
  { }

  public InverseComparer(Comparison<object?> comparison)
  {
    _comparison = Argument.That.NotNull(comparison);
  }

  public InverseComparer(IComparer comparer)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
  }

  #endregion
  #region Methods

  public int Compare(object? x, object? y)
    => (_comparison ?? Comparer.Default.Compare)(y, x);

  #endregion
}
