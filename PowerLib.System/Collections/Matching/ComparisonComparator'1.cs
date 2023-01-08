using System;
using System.Collections.Generic;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Collections.Generic.Extensions;
using PowerLib.System.Collections.NonGeneric;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Matching;

public sealed class ComparisonComparator<T> : IComparator<T>, IComparator
{
  private readonly Comparison<T?> _comparison;

  #region Constructors

  public ComparisonComparator(T? value, Comparison<T?> comparison)
  {
    _comparison = Argument.That.NotNull(comparison);
    Value = value;
  }

  public ComparisonComparator(T? value, IComparer<T> comparer)
  {
    _comparison = Argument.That.NotNull(comparer).AsComparison();
    Value = value;
  }

  #endregion
  #region Properties

  public T? Value { get; }

  #endregion
  #region Methods

  public int Compare(T? obj)
    => _comparison(Value, obj);

  #endregion
  #region Interfaces implementations

  int IComparator.Compare(object? obj)
    => Compare(Argument.That.OfType<T>(obj));

  #endregion
}
