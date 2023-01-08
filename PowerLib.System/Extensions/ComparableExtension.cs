using System;
using PowerLib.System.Validation;

namespace PowerLib.System.Extensions
{
  public static class ComparableExtension
  {
    #region IComparable<T> methods
    #region CompareTo methods

    public static int CompareTo<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : IComparable<T>
    {
      Argument.That.NotNull(value);

      return other is not null ? value.CompareTo(other) : nullOrder == RelativeOrder.Lower ? 1 : -1;
    }

    public static int CompareTo<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : struct, IComparable<T>
    {
      return other is not null ? value.CompareTo(other.Value) : nullOrder == RelativeOrder.Lower ? 1 : nullOrder == RelativeOrder.Upper ? -1 : Operation.That.Failed<int>();
    }

    public static bool MatchTo<T>(this T value, T? other, ComparisonCriteria criteria)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), criteria);

    public static bool MatchTo<T>(this T value, T? other, RelativeOrder nullOrder, ComparisonCriteria criteria)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), criteria);

    public static bool MatchTo<T>(this T value, T? other, RelativeOrder nullOrder, ComparisonCriteria criteria)
      where T : struct, IComparable<T>
      => Comparison.Match(value.CompareTo(other, nullOrder), criteria);

    #endregion
    #region Greater than methods

    public static bool GreaterThan<T>(this T value, T? other)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.GreaterThan);

    public static bool GreaterThan<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.GreaterThan);

    public static bool GreaterThan<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : struct, IComparable<T>
      => Comparison.Match(value.CompareTo(other, nullOrder), ComparisonCriteria.GreaterThan);

    #endregion
    #region Greater than or equal methods

    public static bool GreaterThanOrEqual<T>(this T value, T? other)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.GreaterThanOrEqual);

    public static bool GreaterThanOrEqual<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.GreaterThanOrEqual);

    public static bool GreaterThanOrEqual<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : struct, IComparable<T>
      => Comparison.Match(value.CompareTo(other, nullOrder), ComparisonCriteria.GreaterThanOrEqual);

    #endregion
    #region Less than methods

    public static bool LessThan<T>(this T value, T? other)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.LessThan);

    public static bool LessThan<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.LessThan);

    public static bool LessThan<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : struct, IComparable<T>
      => Comparison.Match(value.CompareTo(other, nullOrder), ComparisonCriteria.LessThan);

    #endregion
    #region Less than or equal methods

    public static bool LessThanOrEqual<T>(this T value, T? other)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.LessThanOrEqual);

    public static bool LessThanOrEqual<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.LessThanOrEqual);

    public static bool LessThanOrEqual<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : struct, IComparable<T>
      => Comparison.Match(value.CompareTo(other, nullOrder), ComparisonCriteria.LessThanOrEqual);

    #endregion
    #region Equal methods

    public static bool Equal<T>(this T value, T? other)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.Equal);

    public static bool Equal<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.Equal);

    public static bool Equal<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : struct, IComparable<T>
      => Comparison.Match(value.CompareTo(other, nullOrder), ComparisonCriteria.Equal);

    #endregion
    #region Not equal methods

    public static bool NotEqual<T>(this T value, T? other)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.NotEqual);

    public static bool NotEqual<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.NotEqual);

    public static bool NotEqual<T>(this T value, T? other, RelativeOrder nullOrder)
      where T : struct, IComparable<T>
      => Comparison.Match(value.CompareTo(other, nullOrder), ComparisonCriteria.NotEqual);

    #endregion
    #region Between methods

    public static bool Between<T>(this T value, T? lower, T? upper, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(lower), value.CompareTo(upper), criteria);

    public static bool Between<T>(this T value, T? lower, T? upper, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
      where T : IComparable<T>
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(lower, nullOrder), value.CompareTo(upper, nullOrder), criteria);

    public static bool Between<T>(this T value, T? lower, T? upper, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
      where T : struct, IComparable<T>
      => Comparison.Match(value.CompareTo(lower, nullOrder), value.CompareTo(upper, nullOrder), criteria);

    #endregion
    #endregion
    #region IComparable methods
    #region Compare methods

    public static int CompareTo(this IComparable value, object? other, RelativeOrder nullOrder)
    {
      Argument.That.NotNull(value);

      return other is not null ? value.CompareTo(other) : nullOrder == RelativeOrder.Lower ? 1 : nullOrder == RelativeOrder.Upper ? -1 : Operation.That.Failed<int>();
    }

    public static bool MatchTo(this IComparable value, object? other, ComparisonCriteria criteria)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), criteria);

    public static bool MatchTo(this IComparable value, object? other, RelativeOrder nullOrder, ComparisonCriteria criteria)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), criteria);

    #endregion
    #region Greater than methods

    public static bool GreaterThan(this IComparable value, object? other)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.GreaterThan);

    public static bool GreaterThan(this IComparable value, object? other, RelativeOrder nullOrder)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.GreaterThan);

    #endregion
    #region Greater than or equal methods

    public static bool GreaterThanOrEqual(this IComparable value, object? other)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.GreaterThanOrEqual);

    public static bool GreaterThanOrEqual(this IComparable value, object? other, RelativeOrder nullOrder)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.GreaterThanOrEqual);

    #endregion
    #region Less than methods

    public static bool LessThan(this IComparable value, object? other)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.LessThan);

    public static bool LessThan(this IComparable value, object? other, RelativeOrder nullOrder)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.LessThan);

    #endregion
    #region Less than or equal methods

    public static bool LessThanOrEqual(this IComparable value, object? other)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.LessThanOrEqual);

    public static bool LessThanOrEqual(this IComparable value, object? other, RelativeOrder nullOrder)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.LessThanOrEqual);

    #endregion
    #region Equal methods

    public static bool Equal(this IComparable value, object? other)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.Equal);

    public static bool Equal(this IComparable value, object? other, RelativeOrder nullOrder)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.Equal);

    #endregion
    #region Not equal methods

    public static bool NotEqual(this IComparable value, object? other)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other), ComparisonCriteria.NotEqual);

    public static bool NotEqual(this IComparable value, object? other, RelativeOrder nullOrder)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(other, nullOrder), ComparisonCriteria.NotEqual);

    #endregion
    #region Between methods

    public static bool Between(this IComparable value, object? lower, object? upper, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(lower), value.CompareTo(upper), criteria);

    public static bool Between(this IComparable value, object? lower, object? upper, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
      => Comparison.Match(Argument.That.NotNull(value).CompareTo(lower, nullOrder), value.CompareTo(upper, nullOrder), criteria);

    #endregion
    #endregion
  }
}
