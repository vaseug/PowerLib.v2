using System;
using System.Collections;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.NonGeneric.Extensions;

public static class ComparerExtension
{
  #region Compare methods

  public static int Compare(this IComparer comparer, object? xValue, object? yValue, RelativeOrder nullOrder)
  {
    Argument.That.NotNull(comparer);

    return xValue is not null ? yValue is not null ? comparer.Compare(xValue, yValue) :
      nullOrder == RelativeOrder.Lower ? 1 : nullOrder == RelativeOrder.Upper ? -1 : Operation.That.Failed<int>() :
      yValue is not null ? nullOrder == RelativeOrder.Lower ? -1 : nullOrder == RelativeOrder.Upper ? 1 : Operation.That.Failed<int>() : 0;
  }

  public static bool Match(this IComparer comparer, object? xValue, object? yValue, ComparisonCriteria criteria)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), criteria);

  public static bool Match(this IComparer comparer, object? xValue, object? yValue, RelativeOrder nullOrder, ComparisonCriteria criteria)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), criteria);

  public static Comparison<object?> AsComparison(this IComparer comparer)
    => Argument.That.NotNull(comparer).Compare;

  public static Comparison<object> AsNonNullComparison(this IComparer comparer)
    => Argument.That.NotNull(comparer).Compare;

  #endregion
  #region Greater than methods

  public static bool GreaterThan(this IComparer comparer, object? xValue, object? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.GreaterThan);

  public static bool GreaterThan(this IComparer comparer, object? xValue, object? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThan);

  #endregion
  #region Greater than or equal methods

  public static bool GreaterThanOrEqual(this IComparer comparer, object? xValue, object? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.GreaterThanOrEqual);

  public static bool GreaterThanOrEqual(this IComparer comparer, object? xValue, object? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThanOrEqual);

  #endregion
  #region Less than methods

  public static bool LessThan(this IComparer comparer, object? xValue, object? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.LessThan);

  public static bool LessThan(this IComparer comparer, object? xValue, object? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThan);

  #endregion
  #region Less than or equal methods

  public static bool LessThanOrEqual(this IComparer comparer, object? xValue, object? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.LessThanOrEqual);

  public static bool LessThanOrEqual(this IComparer comparer, object? xValue, object? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThanOrEqual);

  #endregion
  #region Equal methods

  public static bool Equal(this IComparer comparer, object? xValue, object? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.Equal);

  public static bool Equal(this IComparer comparer, object? xValue, object? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.Equal);

  #endregion
  #region Not equal methods

  public static bool NotEqual(this IComparer comparer, object? xValue, object? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.NotEqual);

  public static bool NotEqual(this IComparer comparer, object? xValue, object? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.NotEqual);

  #endregion
  #region Between methods

  public static bool Between(this IComparer comparer, object? value, object? lowerValue, object? upperValue, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(value, lowerValue), comparer.Compare(value, upperValue), criteria);

  public static bool Between(this IComparer comparer, object? value, object? lowerValue, object? upperValue, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(value, lowerValue, nullOrder), comparer.Compare(value, upperValue, nullOrder), criteria);

  #endregion
  #region Max methods

  public static object? Max(this IComparer comparer, params object?[] values)
  {
    Argument.That.NotNull(comparer);
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(comparer.Compare(result, values[i]), ComparisonCriteria.LessThan))
        result = values[i];
    return result;
  }

  public static object? Max(this IComparer comparer, RelativeOrder nullOrder, params object?[] values)
  {
    Argument.That.NotNull(comparer);
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(comparer.Compare(result, values[i], nullOrder), ComparisonCriteria.LessThan))
        result = values[i];
    return result;
  }

  #endregion
  #region Min methods

  public static object? Min(this IComparer comparer, params object?[] values)
  {
    Argument.That.NotNull(comparer);
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(comparer.Compare(result, values[i]), ComparisonCriteria.GreaterThan))
        result = values[i];
    return result;
  }

  public static object? Min(this IComparer comparer, RelativeOrder nullOrder, params object?[] values)
  {
    Argument.That.NotNull(comparer);
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(comparer.Compare(result, values[i], nullOrder), ComparisonCriteria.GreaterThan))
        result = values[i];
    return result;
  }

  #endregion
}
