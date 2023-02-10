#if NETCOREAPP1_0_OR_GREATER

using System.Buffers;

#else

using System;

#endif

using PowerLib.System.Validation;


namespace PowerLib.System.Buffers;

#if NETCOREAPP1_0_OR_GREATER

internal static class ArrayBuffer<T>
{
  internal static readonly ArrayPool<T> ArrayPool = ArrayPool<T>.Create();
}

#endif

internal static class ArrayBuffer
{
  public static T[] Acquire<T>(int length)
  {
    Argument.That.NonNegative(length);

#if NETCOREAPP1_0_OR_GREATER
    return ArrayBuffer<T>.ArrayPool.Rent(length);
#else
    return new T[length];
#endif
  }

  public static void Release<T>(T[] array, bool clear = false)
  {
    Argument.That.NotNull(array);

#if NETCOREAPP1_0_OR_GREATER
    ArrayBuffer<T>.ArrayPool.Return(array, clear);
#else
    if (clear && array.Length > 0)
      Array.Clear(array, 0, array.Length);
#endif
  }
}
