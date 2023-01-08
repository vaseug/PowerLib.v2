using System.Diagnostics.CodeAnalysis;
using PowerLib.System.Validation;

namespace PowerLib.System
{
  public static class Hasher
  {
    public static Hasher<T?> AsHasher<T>(this Hasher<T> hasher, int defaultHashCode = default)
    {
      Argument.That.NotNull(hasher);

      return ([DisallowNull] T value) => value is null ? defaultHashCode : hasher.Invoke(value);
    }

    public static Hasher<T?> AsNullableHasher<T>(this Hasher<T> hasher, int defaultHashCode = default)
      where T : struct
    {
      Argument.That.NotNull(hasher);

      return ([DisallowNull] T? value) => value is null ? defaultHashCode : hasher.Invoke(value.Value);
    }

    public static Hasher<T> AsNonNullHasher<T>(this Hasher<T?> hasher)
      where T : struct
    {
      Argument.That.NotNull(hasher);

      return value => hasher.Invoke(value);
    }

    public static Hasher<T> AsNonNullableHasher<T>(this Hasher<T?> hasher)
      where T : struct
    {
      Argument.That.NotNull(hasher);

      return value => hasher.Invoke(value);
    }
  }
}
