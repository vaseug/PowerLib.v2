using System;
using PowerLib.System.Validation;

namespace PowerLib.System
{
  public static class Equatable
  {
    public static bool Equals<T>(T? xValue, T? yValue)
      where T : IEquatable<T>
      => xValue is not null && yValue is not null && xValue.Equals(yValue) || xValue is null && yValue is null;

    public static bool Equals<T>(T? xValue, T? yValue)
      where T : struct, IEquatable<T>
      => xValue is not null && yValue is not null && xValue.Value.Equals(yValue.Value) || xValue is null && yValue is null;

    public static bool Equals<T>(T? xValue, T? yValue, bool nullVarious)
      where T : IEquatable<T>
      => xValue is not null && yValue is not null && xValue.Equals(yValue) || xValue is null && yValue is null && !nullVarious;

    public static bool Equals<T>(T? xValue, T? yValue, bool nullVarious)
      where T : struct, IEquatable<T>
      => xValue is not null && yValue is not null && xValue.Value.Equals(yValue.Value) || xValue is null && yValue is null && !nullVarious;

    public static Predicate<T> AsPredicate<T>(T? value)
      where T : IEquatable<T>
      => obj => Equals(obj, value);
  }
}
