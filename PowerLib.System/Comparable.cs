using System;
using PowerLib.System.Extensions;
using PowerLib.System.Validation;

namespace PowerLib.System;

public static class Comparable
{
  #region IComparable<T> methods
  #region Compare methods

  public static int Compare<T>(T? xValue, T? yValue)
    where T : IComparable<T>
    => xValue is not null ? xValue.CompareTo(yValue) : yValue is not null ? -yValue.CompareTo(xValue) : 0;

  public static int Compare<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : IComparable<T>
    => xValue is not null ? xValue.CompareTo(yValue, nullOrder) : yValue is not null ? -yValue.CompareTo(xValue, nullOrder) : 0;

  public static int Compare<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct, IComparable<T>
    => xValue is not null ? xValue.Value.CompareTo(yValue, nullOrder) : yValue is not null ? -yValue.Value.CompareTo(xValue, nullOrder) : 0;

  public static bool Match<T>(T? xValue, T? yValue, ComparisonCriteria criteria)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue), criteria);

  public static bool Match<T>(T? xValue, T? yValue, RelativeOrder nullOrder, ComparisonCriteria criteria)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), criteria);

  public static bool Match<T>(T? xValue, T? yValue, RelativeOrder nullOrder, ComparisonCriteria criteria)
    where T : struct, IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), criteria);

  #endregion
  #region Greater than methods

  public static bool GreaterThan<T>(T? xValue, T? yValue)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.GreaterThan);

  public static bool GreaterThan<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThan);

  public static bool GreaterThan<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct, IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThan);

  #endregion
  #region Greater than or equal methods

  public static bool GreaterThanOrEqual<T>(T? xValue, T? yValue)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.GreaterThanOrEqual);

  public static bool GreaterThanOrEqual<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThanOrEqual);

  public static bool GreaterThanOrEqual<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct, IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThanOrEqual);

  #endregion
  #region Less than methods

  public static bool LessThan<T>(T? xValue, T? yValue)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.LessThan);

  public static bool LessThan<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThan);

  public static bool LessThan<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct, IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThan);

  #endregion
  #region Less than or equal methods

  public static bool LessThanOrEqual<T>(T? xValue, T? yValue)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.LessThanOrEqual);

  public static bool LessThanOrEqual<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThanOrEqual);

  public static bool LessThanOrEqual<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct, IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThanOrEqual);

  #endregion
  #region Equal methods

  public static bool Equal<T>(T? xValue, T? yValue)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.Equal);

  public static bool Equal<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.Equal);

  public static bool Equal<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct, IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.Equal);

  #endregion
  #region Not equal methods

  public static bool NotEqual<T>(T? xValue, T? yValue)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.NotEqual);

  public static bool NotEqual<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.NotEqual);

  public static bool NotEqual<T>(T? xValue, T? yValue, RelativeOrder nullOrder)
    where T : struct, IComparable<T>
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.NotEqual);

  #endregion
  #region Between methods

  public static bool Between<T>(T? value, T? lower, T? upper, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
    where T : IComparable<T>
    => Comparison.Match(Compare(value, lower), Compare(value, upper), criteria);

  public static bool Between<T>(T? value, T? lower, T? upper, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
    where T : IComparable<T?>
    => Comparison.Match(Compare(value, lower, nullOrder), Compare(value, upper, nullOrder), criteria);

  public static bool Between<T>(T? value, T? lower, T? upper, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
    where T : struct, IComparable<T>
    => Comparison.Match(Compare(value, lower, nullOrder), Compare(value, upper, nullOrder), criteria);

  #endregion
  #region Max methods

  public static T? Max<T>(params T?[] values)
    where T : IComparable<T>
  {
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(Compare(result, values[i]), ComparisonCriteria.LessThan))
        result = values[i];
    return result;
  }

  public static T? Max<T>(RelativeOrder nullOrder, params T?[] values)
    where T : IComparable<T>
  {
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(Compare(result, values[i], nullOrder), ComparisonCriteria.LessThan))
        result = values[i];
    return result;
  }

  public static T? Max<T>(RelativeOrder nullOrder, params T?[] values)
    where T : struct, IComparable<T>
  {
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(Compare(result, values[i], nullOrder), ComparisonCriteria.LessThan))
        result = values[i];
    return result;
  }

  #endregion
  #region Min methods

  public static T? Min<T>(params T?[] values)
    where T : IComparable<T>
  {
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(Compare(result, values[i]), ComparisonCriteria.GreaterThan))
        result = values[i];
    return result;
  }

  public static T? Min<T>(RelativeOrder nullOrder, params T?[] values)
    where T : IComparable<T>
  {
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(Compare(result, values[i], nullOrder), ComparisonCriteria.GreaterThan))
        result = values[i];
    return result;
  }

  public static T? Min<T>(RelativeOrder nullOrder, params T?[] values)
    where T : struct, IComparable<T>
  {
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(Compare(result, values[i], nullOrder), ComparisonCriteria.GreaterThan))
        result = values[i];
    return result;
  }

  #endregion
  #region Predicate

  public static Predicate<T> AsPredicate<T>(T? value, ComparisonCriteria criteria)
    where T : IComparable<T>
    => obj => Match(obj, value, criteria);

  public static Predicate<T> AsPredicate<T>(T? lowerValue, T? upperValue, BetweenCriteria criteria)
    where T : IComparable<T>
    => obj => Between(obj, lowerValue, upperValue, criteria);

  #endregion
  #endregion
  #region IComparable methods
  #region Compare methods

  public static int Compare(IComparable? xValue, IComparable? yValue)
    => xValue is not null ? xValue.CompareTo(yValue) : yValue is not null ? -yValue.CompareTo(xValue) : 0;

  public static int Compare(IComparable? xValue, IComparable? yValue, RelativeOrder nullOrder)
    => xValue is not null ? xValue.CompareTo(yValue, nullOrder) : yValue is not null ? -yValue.CompareTo(xValue) : 0;

  public static bool Match(IComparable? xValue, IComparable? yValue, ComparisonCriteria criteria)
    => Comparison.Match(Compare(xValue, yValue), criteria);

  public static bool Match(IComparable? xValue, IComparable? yValue, RelativeOrder nullOrder, ComparisonCriteria criteria)
    => Comparison.Match(Compare(xValue, yValue, nullOrder), criteria);

  #endregion
  #region Greater than methods

  public static bool GreaterThan(IComparable? xValue, IComparable? yValue)
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.GreaterThan);

  public static bool GreaterThan(IComparable? xValue, IComparable? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThan);

  #endregion
  #region Greater than or equal methods

  public static bool GreaterThanOrEqual(IComparable? xValue, IComparable? yValue)
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.GreaterThanOrEqual);

  public static bool GreaterThanOrEqual(IComparable? xValue, IComparable? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.GreaterThanOrEqual);

  #endregion
  #region Less than methods

  public static bool LessThan(IComparable? xValue, IComparable? yValue)
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.LessThan);

  public static bool LessThan(IComparable? xValue, IComparable? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThan);

  #endregion
  #region Less than or equal methods

  public static bool LessThanOrEqual(IComparable? xValue, IComparable? yValue)
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.LessThanOrEqual);

  public static bool LessThanOrEqual(IComparable? xValue, IComparable? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.LessThanOrEqual);

  #endregion
  #region Equal methods

  public static bool Equal(IComparable? xValue, IComparable? yValue)
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.Equal);

  public static bool Equal(IComparable? xValue, IComparable? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.Equal);

  #endregion
  #region Not equal methods

  public static bool NotEqual(IComparable? xValue, IComparable? yValue)
    => Comparison.Match(Compare(xValue, yValue), ComparisonCriteria.NotEqual);

  public static bool NotEqual(IComparable? xValue, IComparable? yValue, RelativeOrder nullOrder)
    => Comparison.Match(Compare(xValue, yValue, nullOrder), ComparisonCriteria.NotEqual);

  #endregion
  #region Between methods

  public static bool Between(IComparable? value, IComparable? lower, IComparable? upper, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
    => Comparison.Match(Compare(value, lower), Compare(value, upper), criteria);

  public static bool Between(IComparable? value, IComparable? lower, IComparable? upper, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
    => Comparison.Match(Compare(value, lower, nullOrder), Compare(value, upper, nullOrder), criteria);

  #endregion
  #region Max methods

  public static IComparable? Max(params IComparable?[] values)
  {
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(Compare(result, values[i]), ComparisonCriteria.LessThan))
        result = values[i];
    return result;
  }

  public static IComparable? Max(RelativeOrder nullOrder, params IComparable?[] values)
  {
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(Compare(result, values[i], nullOrder), ComparisonCriteria.LessThan))
        result = values[i];
    return result;
  }

  #endregion
  #region Min methods

  public static IComparable? Min(params IComparable?[] values)
  {
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(Compare(result, values[i]), ComparisonCriteria.GreaterThan))
        result = values[i];
    return result;
  }

  public static IComparable? Min(RelativeOrder nullOrder, params IComparable?[] values)
  {
    Argument.That.NotEmpty(values);

    var result = values[0];
    for (int i = 1; i < values.Length; i++)
      if (Comparison.Match(Compare(result, values[i], nullOrder), ComparisonCriteria.GreaterThan))
        result = values[i];
    return result;
  }

  #endregion
  #endregion
}
