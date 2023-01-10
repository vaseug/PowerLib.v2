using System;
using PowerLib.System.Validation;

namespace PowerLib.System
{
  public static class Comparison
  {
    internal static bool Match(int result, ComparisonCriteria criteria)
      => criteria switch
      {
        ComparisonCriteria.Equal => result == 0,
        ComparisonCriteria.NotEqual => result != 0,
        ComparisonCriteria.GreaterThan => result > 0,
        ComparisonCriteria.GreaterThanOrEqual => result >= 0,
        ComparisonCriteria.LessThan => result < 0,
        ComparisonCriteria.LessThanOrEqual => result <= 0,
        _ => Operation.That.Failed()
      };

    internal static bool Match(int lowerResult, int upperResult, BetweenCriteria criteria)
      => criteria switch
      {
        BetweenCriteria.IncludeBoth => lowerResult >= 0 && upperResult <= 0,
        BetweenCriteria.ExcludeLower => lowerResult > 0 && upperResult <= 0,
        BetweenCriteria.ExcludeUpper => lowerResult >= 0 && upperResult < 0,
        BetweenCriteria.ExcludeBoth => lowerResult > 0 && upperResult < 0,
        _ => Operation.That.Failed()
      };

    internal static int Result(int result, int offset, bool xSuccess, bool ySuccess, RelativeOrder emptyOrder)
      => xSuccess ? ySuccess ? (result > 0 ? offset + 1 : result < 0 ? -(offset + 1) : 0) :
        emptyOrder switch { RelativeOrder.Lower => offset + 1, RelativeOrder.Upper => -(offset + 1), _ => Argument.That.Invalid(emptyOrder) } :
        ySuccess ? emptyOrder switch { RelativeOrder.Lower => -(offset + 1), RelativeOrder.Upper => offset + 1, _ => Argument.That.Invalid(emptyOrder) } : 0;

    public static int Invoke<T>(this Comparison<T> comparison, T? xValue, T? yValue, RelativeOrder nullOrder)
    {
      Argument.That.NotNull(comparison);

      return xValue is not null ? yValue is not null ? comparison(xValue, yValue) :
        nullOrder == RelativeOrder.Lower ? 1 : nullOrder == RelativeOrder.Upper ? -1 : Argument.That.Invalid(nullOrder) : yValue is not null ? nullOrder == RelativeOrder.Lower ? -1 : nullOrder == RelativeOrder.Upper ? 1 : Argument.That.Invalid(nullOrder) : 0;
    }

    public static int Invoke<T>(this Comparison<T> comparison, T? xValue, T? yValue, RelativeOrder nullOrder)
      where T : struct
    {
      Argument.That.NotNull(comparison);

      return xValue is not null ? yValue is not null ? comparison(xValue.Value, yValue.Value) :
        nullOrder == RelativeOrder.Lower ? 1 : nullOrder == RelativeOrder.Upper ? -1 : Argument.That.Invalid(nullOrder) : yValue is not null ? nullOrder == RelativeOrder.Lower ? -1 : nullOrder == RelativeOrder.Upper ? 1 : Argument.That.Invalid(nullOrder) : 0;
    }

    public static int Invoke<T>(this Comparison<T?> comparison, TryOut<T> xItem, TryOut<T> yItem, RelativeOrder emptyOrder)
      => xItem.Success ? yItem.Success ? Argument.That.NotNull(comparison)(xItem.Value, yItem.Value) :
        emptyOrder switch { RelativeOrder.Lower => 1, RelativeOrder.Upper => -1, _ => Argument.That.Invalid(emptyOrder) } :
        yItem.Success ? emptyOrder switch { RelativeOrder.Lower => -1, RelativeOrder.Upper => 1, _ => Argument.That.Invalid(emptyOrder) } :
        0;

    public static bool Match<T>(this Comparison<T> comparison, T xValue, T yValue, ComparisonCriteria criteria)
      => Match(Argument.That.NotNull(comparison).Invoke(xValue, yValue), criteria);

    public static bool Match<T>(this Comparison<T> comparison, T? xValue, T? yValue, RelativeOrder nullOrder, ComparisonCriteria criteria)
      => Match(Invoke(Argument.That.NotNull(comparison), xValue, yValue, nullOrder), criteria);

    public static bool Match<T>(this Comparison<T> comparison, T? xValue, T? yValue, RelativeOrder nullOrder, ComparisonCriteria criteria)
      where T : struct
      => Match(Invoke(Argument.That.NotNull(comparison), xValue, yValue, nullOrder), criteria);

    public static bool Match<T>(this Comparison<T> comparison, T value, T lowerValue, T upperValue, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)

      => Match(Argument.That.NotNull(comparison).Invoke(value, lowerValue), comparison.Invoke(value, upperValue), criteria);

    public static bool Match<T>(this Comparison<T> comparison, T? value, T? lowerValue, T? upperValue, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
      => Match(Invoke(Argument.That.NotNull(comparison), value, lowerValue, nullOrder), Invoke(Argument.That.NotNull(comparison), value, upperValue, nullOrder), criteria);

    public static bool Match<T>(this Comparison<T> comparison, T? value, T? lowerValue, T? upperValue, RelativeOrder nullOrder, BetweenCriteria criteria = BetweenCriteria.IncludeBoth)
      where T : struct
      => Match(Invoke(Argument.That.NotNull(comparison), value, lowerValue, nullOrder), Invoke(Argument.That.NotNull(comparison), value, upperValue, nullOrder), criteria);

    public static Comparison<T?> AsComparison<T>(this Comparison<T> comparison)
      => comparison.AsComparison(RelativeOrder.Lower);

    public static Comparison<T?> AsComparison<T>(this Comparison<T> comparison, RelativeOrder nullOrder)
    {
      Argument.That.NotNull(comparison);

      return (xValue, yValue) => comparison.Invoke(xValue, yValue, nullOrder);
    }

    public static Comparison<T?> AsNullableComparison<T>(this Comparison<T> comparison)
      where T : struct
      => comparison.AsNullableComparison(RelativeOrder.Lower);

    public static Comparison<T?> AsNullableComparison<T>(this Comparison<T> comparison, RelativeOrder nullOrder)
      where T : struct
    {
      Argument.That.NotNull(comparison);

      return (xValue, yValue) => comparison.Invoke(xValue, yValue, nullOrder);
    }

    public static Comparison<T> AsNonNullComparison<T>(this Comparison<T?> comparison)
      => (Comparison<T>)Argument.That.NotNull(comparison);

    public static Comparison<T> AsNonNullableComparison<T>(this Comparison<T?> comparison)
      where T : struct
    {
      Argument.That.NotNull(comparison);

      return (xValue, yValue) => comparison(xValue, yValue);
    }

    public static Equality<T> AsEquality<T>(this Comparison<T> comparison, ComparisonCriteria criteria)
    {
      Argument.That.NotNull(comparison);

      return (xValue, yValue) => Match(comparison(xValue, yValue), criteria);
    }

    public static Equality<T?> AsEquality<T>(this Comparison<T> comparison, RelativeOrder nullOrder, ComparisonCriteria criteria)
    {
      Argument.That.NotNull(comparison);

      return (xValue, yValue) => Match(Invoke(comparison, xValue, yValue, nullOrder), criteria);
    }

    public static Equality<T?> AsNullableEquality<T>(this Comparison<T> comparison, RelativeOrder nullOrder, ComparisonCriteria criteria)
      where T : struct
    {
      Argument.That.NotNull(comparison);

      return (xValue, yValue) => Match(Invoke(comparison, xValue, yValue, nullOrder), criteria);
    }

    public static Predicate<T> AsPredicate<T>(this Comparison<T> comparison, T value, ComparisonCriteria criteria)
    {
      Argument.That.NotNull(comparison);

      return obj => comparison.Match(obj, value, criteria);
    }

    public static Predicate<T> AsPredicate<T>(this Comparison<T> comparison, T lowerValue, T upperValue, BetweenCriteria criteria)
    {
      Argument.That.NotNull(comparison);

      return obj => comparison.Match(obj, lowerValue, upperValue, criteria);
    }
  }
}
