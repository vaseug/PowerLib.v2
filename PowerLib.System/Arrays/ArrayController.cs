#if NETCOREAPP1_0_OR_GREATER

using System.Buffers;

#else

using System;

#endif

using PowerLib.System.Validation;


namespace PowerLib.System.Arrays;

internal static class ArrayController
{
  public static bool ClearArray { get; set; }

  public static void Release<T>(T[] array)
    => Release(array, ClearArray);

  public static T[] Acquire<T>(int length)
  {
    Argument.That.NonNegative(length);

#if NETCOREAPP1_0_OR_GREATER
    return ArrayPool<T>.Shared.Rent(length);
#else
    return new T[length];
#endif
  }

  public static void Release<T>(T[] array, bool clear)
  {
    Argument.That.NotNull(array);

#if NETCOREAPP1_0_OR_GREATER
    ArrayPool<T>.Shared.Return(array, clear);
#else
    if (clear && array.Length > 0)
      Array.Clear(array, 0, array.Length);
#endif
  }
}
