using System;
using PowerLib.System.Validation;

namespace PowerLib.System.Arrays.Extensions;

public static class RegularArrayExtension
{
  #region Regular array
  #region Miscellaneous regular extensions

  public static Type GetRegularArrayElementType(this Array array)
  {
    Argument.That.NotNull(array);

    return array.GetType().GetElementType()!;
  }

  public static int[] GetRegularArrayLowerBounds(this Array array)
  {
    Argument.That.NotNull(array);

    var lowerBounds = new int[array.Rank];
    for (var i = 0; i < array.Rank; i++)
      lowerBounds[i] = array.GetLowerBound(i);
    return lowerBounds;
  }

  public static long[] GetRegularArrayLongLowerBounds(this Array array)
  {
    Argument.That.NotNull(array);

    var lowerBounds = new long[array.Rank];
    for (var i = 0; i < array.Rank; i++)
      lowerBounds[i] = 0L;
    return lowerBounds;
  }

  public static int[] GetRegularArrayLengths(this Array array)
  {
    Argument.That.NotNull(array);

    var lengths = new int[array.Rank];
    for (var i = 0; i < array.Rank; i++)
      lengths[i] = array.GetLength(i);
    return lengths;
  }

  public static long[] GetRegularArrayLongLengths(this Array array)
  {
    Argument.That.NotNull(array);

    var lengths = new long[array.Rank];
    for (var i = 0; i < array.Rank; i++)
      lengths[i] = array.GetLongLength(i);
    return lengths;
  }

  public static ArrayDimension[] GetRegularArrayDimensions(this Array array, (int index, int count)[]? ranges = null)
    => array.GetRegularArrayDimensions(false, ranges);

  public static ArrayDimension[] GetRegularArrayDimensions(this Array array, bool zeroBased, (int index, int count)[]? ranges = null)
  {
    Argument.That.NotNull(array);
    if (ranges is not null)
      Argument.That.InRangeOut(array, ranges, zeroBased);

    var dimensions = new ArrayDimension[array.Rank];
    for (var i = 0; i < array.Rank; i++)
      dimensions[i] = ranges is not null ?
        new ArrayDimension(ranges[i].count, ranges[i].index + (zeroBased ? array.GetLowerBound(i) : 0)) :
        new ArrayDimension(array.GetLength(i));
    return dimensions;
  }

  public static ArrayLongDimension[] GetRegularArrayLongDimensions(this Array array, (long index, long count)[]? ranges = null)
  {
    Argument.That.NotNull(array);
    if (ranges is not null)
      Argument.That.InRangeOut(array, ranges);

    var dimensions = new ArrayLongDimension[array.Rank];
    for (var i = 0; i < array.Rank; i++)
      dimensions[i] = ranges is not null ?
        new ArrayLongDimension(ranges[i].count, ranges[i].index) :
        new ArrayLongDimension(array.GetLongLength(i), 0L);
    return dimensions;
  }

  #endregion
  #endregion
}
