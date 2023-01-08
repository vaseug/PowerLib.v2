using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class ObjectComparer<T> : IComparer<T>, IComparer
  where T : class
{
  private readonly Comparison<T?>? _comparison;

  private static readonly Lazy<ObjectComparer<T>> instanceNullLower = new(() => new ObjectComparer<T>(RelativeOrder.Lower));
  private static readonly Lazy<ObjectComparer<T>> instanceNullUpper = new(() => new ObjectComparer<T>(RelativeOrder.Upper));

  #region Constructors

  private ObjectComparer(RelativeOrder nullOrder)
  {
    _comparison = default;
    NullOrder = nullOrder;
  }

  public ObjectComparer(Comparison<T?> comparison, RelativeOrder nullOrder)
  {
    _comparison = Argument.That.NotNull(comparison);
    NullOrder = nullOrder;
  }

  public ObjectComparer(IComparer<T> comparer, RelativeOrder nullOrder)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
    NullOrder = nullOrder;
  }

  #endregion
  #region Properties

  public static ObjectComparer<T> DefaultNullAhead
    => instanceNullLower.Value;

  public static ObjectComparer<T> DefaultNullBehind
    => instanceNullUpper.Value;

  public RelativeOrder NullOrder { get; }

  #endregion
  #region Methods

  public int Compare(T? x, T? y)
    => x is not null ? y is not null ? (_comparison ?? Comparer<T>.Default.AsComparison<T>())(x, y) : NullOrder == RelativeOrder.Upper ? -1 : 1 : y is not null ? NullOrder == RelativeOrder.Upper ? 1 : -1 : 0;

  #endregion
  #region Interfaces implementations

  int IComparer.Compare(object? x, object? y)
    => Compare(Argument.That.OfType<T>(x), Argument.That.OfType<T>(y));

  #endregion
}
