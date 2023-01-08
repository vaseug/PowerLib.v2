using System;
using System.Collections;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class NullableComparer<T> : IComparer<T?>, IComparer
  where T : struct
{
  private readonly Comparison<T>? _comparison;

  private static readonly Lazy<NullableComparer<T>> instanceNullLower = new(() => new NullableComparer<T>(RelativeOrder.Lower));
  private static readonly Lazy<NullableComparer<T>> instanceNullUpper = new(() => new NullableComparer<T>(RelativeOrder.Upper));

  #region Constructors

  private NullableComparer(RelativeOrder nullOrder)
  {
    _comparison = default;
    NullOrder = nullOrder;
  }

  public NullableComparer(Comparison<T> comparison, RelativeOrder nullOrder)
  {
    _comparison = Argument.That.NotNull(comparison);
    NullOrder = nullOrder;
  }

  public NullableComparer(IComparer<T> comparer, RelativeOrder nullOrder)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
    NullOrder = nullOrder;
  }

  #endregion
  #region Properties

  public static NullableComparer<T> DefaultNullLower
    => instanceNullLower.Value;

  public static NullableComparer<T> DefaultNullUpper
    => instanceNullUpper.Value;

  public RelativeOrder NullOrder { get; }

  #endregion
  #region Methods

  public int Compare(T? x, T? y)
    => x is not null ? y is not null ? (_comparison ?? Comparer<T>.Default.Compare)(x.Value, y.Value) : NullOrder == RelativeOrder.Upper ? -1 : 1 : y is not null ? NullOrder == RelativeOrder.Upper ? 1 : -1 : 0;

  #endregion
  #region Interfaces implementations

  int IComparer.Compare(object? x, object? y)
    => Compare(Argument.That.OfType<T?>(x), Argument.That.OfType<T?>(y));

  #endregion
}
