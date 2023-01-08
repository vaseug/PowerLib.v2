using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class SequenceComparer : IComparer<IEnumerable>, IComparer
{
  private readonly Comparison<object?>? _сomparison;

  private static readonly Lazy<SequenceComparer> instance = new(() => new SequenceComparer());

  #region Constructors

  private SequenceComparer()
  { }

  public SequenceComparer(Comparison<object?> сomparison)
  {
    _сomparison = Argument.That.NotNull(сomparison);
  }

  public SequenceComparer(IComparer comparer)
  {
    _сomparison = Argument.That.NotNull(comparer).AsComparison();
  }

  #endregion
  #region Properties

  public static SequenceComparer Default
    => instance.Value;

  #endregion
  #region Methods

  public int Compare(IEnumerable? x, IEnumerable? y)
    => x is not null ? y is not null ? x.OfType<object?>().SequenceCompare(y.OfType<object?>(), _сomparison) : 1 : y is not null ? -1 : 0;

  #endregion
  #region Interfaces implementation

  int IComparer.Compare(object? x, object? y)
    => Compare(Argument.That.OfType<IEnumerable>(x), Argument.That.OfType<IEnumerable>(y));

  #endregion
}
