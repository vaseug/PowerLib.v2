using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class InverseComparer<T> : IComparer<T>, IComparer
{
  private readonly Comparison<T?>? _comparison;

  private static readonly Lazy<InverseComparer<T>> instance = new(() => new InverseComparer<T>());

  #region Constructors

  private InverseComparer()
  { }

  public InverseComparer(Comparison<T?> comparison)
  {
    _comparison = Argument.That.NotNull(comparison);
  }

  public InverseComparer(IComparer<T> comparer)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
  }

  #endregion
  #region Properties

  public InverseComparer<T> Default
    => instance.Value;

  #endregion
  #region Methods

  public int Compare(T? x, T? y)
    => (_comparison ?? Comparer<T>.Default.AsComparison<T>())(y, x);

  #endregion
  #region Interfaces implementations

  int IComparer.Compare(object? x, object? y)
    => Compare(Argument.That.OfType<T>(x), Argument.That.OfType<T>(y));

  #endregion
}
