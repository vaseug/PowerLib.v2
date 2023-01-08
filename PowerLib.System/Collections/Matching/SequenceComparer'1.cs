using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class SequenceComparer<T> : IComparer<IEnumerable<T>>, IComparer
{
  private readonly Comparison<T?>? _сomparison;

  private static readonly Lazy<SequenceComparer<T>> instance = new(() => new SequenceComparer<T>());

  #region Constructors

  private SequenceComparer()
  { }

  public SequenceComparer(Comparison<T?> сomparison)
  {
    _сomparison = Argument.That.NotNull(сomparison);
  }

  public SequenceComparer(IComparer<T> comparer)
  {
    _сomparison = Argument.That.NotNull(comparer).AsComparison();
  }

  #endregion
  #region Properties

  public static SequenceComparer<T> Default
    => instance.Value;

  #endregion
  #region Methods

  public int Compare(IEnumerable<T>? x, IEnumerable<T>? y)
    => x is not null ? y is not null ? x.SequenceCompare(y, _сomparison) : 1 : y is not null ? -1 : 0;

  #endregion
  #region Interfaces implementation

  int IComparer.Compare(object? x, object? y)
    => Compare(Argument.That.OfType<IEnumerable<T>>(x), Argument.That.OfType<IEnumerable<T>>(y));

  #endregion
}
