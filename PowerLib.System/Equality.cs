using System;
using PowerLib.System.Validation;

namespace PowerLib.System
{
  public static class Equality
  {
    public static bool Invoke<T>(this Equality<T> equality, T? xValue, T? yValue, bool nullVarious)
    {
      Argument.That.NotNull(equality);

      return xValue is null && yValue is null && !nullVarious || xValue is not null && yValue is not null && equality(xValue, yValue);
    }

    public static bool Invoke<T>(this Equality<T> equality, T? xValue, T? yValue, bool nullVarious)
      where T : struct
    {
      Argument.That.NotNull(equality);

      return xValue is null && yValue is null && !nullVarious || xValue is not null && yValue is not null && equality(xValue.Value, yValue.Value);
    }

    public static Equality<T?> AsEquality<T>(this Equality<T> equality)
      => equality.AsEquality(false);

    public static Equality<T?> AsEquality<T>(this Equality<T> equality, bool nullVarious)
    {
      Argument.That.NotNull(equality);

      return (xValue, yValue) => equality.Invoke(xValue, yValue, nullVarious);
    }

    public static Equality<T?> AsNullableEquality<T>(this Equality<T> equality)
      where T : struct
      => equality.AsNullableEquality(false);

    public static Equality<T?> AsNullableEquality<T>(this Equality<T> equality, bool nullVarious)
      where T : struct
    {
      Argument.That.NotNull(equality);

      return (xValue, yValue) => equality.Invoke(xValue, yValue, nullVarious);
    }

    public static Equality<T> AsNonNullEquality<T>(this Equality<T?> equality)
      => (Equality<T>)Argument.That.NotNull(equality);

    public static Equality<T> AsNonNullableEquality<T>(this Equality<T?> equality)
      where T : struct
    {
      Argument.That.NotNull(equality);

      return (xValue, yValue) => equality(xValue, yValue);
    }

    public static Predicate<T> AsPredicate<T>(this Equality<T> equality, T value)
    {
      Argument.That.NotNull(equality);

      return obj => equality(obj, value);
    }
  }
}
