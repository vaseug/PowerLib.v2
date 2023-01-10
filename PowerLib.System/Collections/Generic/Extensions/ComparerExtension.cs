using System;
using System.Collections.Generic;
using PowerLib.System;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Generic.Extensions;

public static class ComparerExtension
{
  #region Compare methods

  public static int Compare<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : class
  {
    Argument.That.NotNull(comparer);

    return xValue is not null ? yValue is not null ? comparer.Compare(xValue, yValue) :
      nullOrder == RelativeOrder.Lower ? 1 : nullOrder == RelativeOrder.Upper ? -1 : Argument.That.Invalid(nullOrder) :
      yValue is not null ? nullOrder == RelativeOrder.Lower ? -1 : nullOrder == RelativeOrder.Upper ? 1 : Argument.That.Invalid(nullOrder) : 0;
  }

  public static int Compare<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct
  {
    Argument.That.NotNull(comparer);

    return xValue is not null ? yValue is not null ? comparer.Compare(xValue.Value, yValue.Value) :
      nullOrder == RelativeOrder.Lower ? 1 : nullOrder == RelativeOrder.Upper ? -1 : Argument.That.Invalid(nullOrder) :
      yValue is not null ? nullOrder == RelativeOrder.Lower ? -1 : nullOrder == RelativeOrder.Upper ? 1 : Argument.That.Invalid(nullOrder) : 0;
  }

  public static bool Match<T>(this IComparer<T> comparer, T? xValue, T? yValue, ComparisonCriteria criteria)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), criteria);

  public static bool Match<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder, ComparisonCriteria criteria)
    where T : class
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), criteria);

  public static bool Match<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder, ComparisonCriteria criteria)
    where T : struct
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), criteria);

  public static Comparison<T?> AsComparison<T>(this IComparer<T> comparer)
    => Argument.That.NotNull(comparer).Compare;

  public static Comparison<T> AsNonNullComparison<T>(this IComparer<T> comparer)
    => Argument.That.NotNull(comparer).Compare;

  public static Comparison<T> AsNonNullableComparison<T>(this IComparer<T?> comparer)
    where T : struct
    => (x, y) => Argument.That.NotNull(comparer).Compare(x, y);

  public static Comparison<T?> AsComparison<T>(this Func<T?, T?, int> function)
    => Argument.That.NotNull(function).Invoke;

  #endregion
  #region Greater than methods

  public static bool GreaterThan<T>(this IComparer<T> comparer, T? xValue, T? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.GreaterThan);

  public static bool GreaterThan<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : class
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThan);

  public static bool GreaterThan<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThan);

  #endregion
  #region Greater than or equal methods

  public static bool GreaterThanOrEqual<T>(this IComparer<T> comparer, T? xValue, T? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.GreaterThanOrEqual);

  public static bool GreaterThanOrEqual<T>(this IComparer<T> comparer, T xValue, T yValue, RelativeOrder nullOrder)
    where T : class
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThanOrEqual);

  public static bool GreaterThanOrEqual<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThanOrEqual);

  #endregion
  #region Less than methods

  public static bool LessThan<T>(this IComparer<T> comparer, T? xValue, T? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.LessThan);

  public static bool LessThan<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : class
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThan);

  public static bool LessThan<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThan);

  #endregion
  #region Less than or equal methods

  public static bool LessThanOrEqual<T>(this IComparer<T> comparer, T? xValue, T? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.LessThanOrEqual);

  public static bool LessThanOrEqual<T>(this IComparer<T> comparer, T xValue, T yValue, RelativeOrder nullOrder)
    where T : class
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThanOrEqual);

  public static bool LessThanOrEqual<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThanOrEqual);

  #endregion
  #region Equal methods

  public static bool Equal<T>(this IComparer<T> comparer, T? xValue, T? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.Equal);

  public static bool Equal<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : class
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.Equal);

  public static bool Equal<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.Equal);

  #endregion
  #region Not equal methods

  public static bool NotEqual<T>(this IComparer<T> comparer, T? xValue, T? yValue)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue), ComparisonCriteria.NotEqual);

  public static bool NotEqual<T>(this IComparer<T> comparer, T xValue, T yValue, RelativeOrder nullOrder)
    where T : class
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.NotEqual);

  public static bool NotEqual<T>(this IComparer<T> comparer, T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(xValue, yValue, nullOrder), ComparisonCriteria.NotEqual);

  #endregion
  #region Between methods

  public static bool Between<T>(this IComparer<T> comparer, T? value, T? lowerValue, T? upperValue, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(value, lowerValue), comparer.Compare(value, upperValue), criteria);

  public static bool Between<T>(this IComparer<T> comparer, T? value, T? lowerValue, T? upperValue, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
    where T : class
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(value, lowerValue, nullOrder), comparer.Compare(value, upperValue, nullOrder), criteria);

  public static bool Between<T>(this IComparer<T> comparer, T? value, T? lowerValue, T? upperValue, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
    where T : struct
    => Comparison.Match(Argument.That.NotNull(comparer).Compare(value, lowerValue, nullOrder), comparer.Compare(value, upperValue, nullOrder), criteria);

  #endregion
  #region Max methods

  public static T? Max<T>(this IComparer<T> comparer, params T?[] values)
  {
    Argument.That.NotNull(comparer);
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(comparer.Compare(result, values[i]), ComparisonCriteria.LessThan))
        result = values[i];
    return result;
  }

  public static T? Max<T>(this IComparer<T> comparer, RelativeOrder nullOrder, params T?[] values)
    where T : class
  {
    Argument.That.NotNull(comparer);
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(comparer.Compare(result, values[i], nullOrder), ComparisonCriteria.LessThan))
        result = values[i];
    return result;
  }

  public static T? Max<T>(this IComparer<T> comparer, RelativeOrder nullOrder, params T?[] values)
    where T : struct
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

  public static T? Min<T>(this IComparer<T> comparer, params T?[] values)
  {
    Argument.That.NotNull(comparer);
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(comparer.Compare(result, values[i]), ComparisonCriteria.GreaterThan))
        result = values[i];
    return result;
  }

  public static T? Min<T>(this IComparer<T> comparer, RelativeOrder nullOrder, params T?[] values)
    where T : class
  {
    Argument.That.NotNull(comparer);
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(comparer.Compare(result, values[i], nullOrder), ComparisonCriteria.GreaterThan))
        result = values[i];
    return result;
  }

  public static T? Min<T>(this IComparer<T> comparer, RelativeOrder nullOrder, params T?[] values)
    where T : struct
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
