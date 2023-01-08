using System;
using System.Collections;
using PowerLib.System.Collections.NonGeneric.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class ObjectComparer : IComparer
{
  private readonly Comparison<object?>? _comparison;

  private static readonly Lazy<ObjectComparer> instanceNullLower = new(() => new ObjectComparer(RelativeOrder.Lower));
  private static readonly Lazy<ObjectComparer> instanceNullUpper = new(() => new ObjectComparer(RelativeOrder.Upper));

  #region Constructors

  private ObjectComparer(RelativeOrder nullOrder)
  {
    _comparison = default;
    NullOrder = nullOrder;
  }

  public ObjectComparer(Comparison<object?> comparison, RelativeOrder nullOrder)
  {
    _comparison = Argument.That.NotNull(comparison);
    NullOrder = nullOrder;
  }

  public ObjectComparer(IComparer comparer, RelativeOrder nullOrder)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
    NullOrder = nullOrder;
  }

  #endregion
  #region Properties

  public static ObjectComparer DefaultNullLower
    => instanceNullLower.Value;

  public static ObjectComparer DefaultNullUpper
    => instanceNullUpper.Value;

  public RelativeOrder NullOrder { get; }

  #endregion
  #region Methods

  public int Compare(object? x, object? y)
    => x is not null ? y is not null ? (_comparison ?? Comparer.Default.Compare)(x, y) : NullOrder == RelativeOrder.Upper ? -1 : 1 : y is not null ? NullOrder == RelativeOrder.Upper ? 1 : -1 : 0;

  #endregion
}
