using System.Linq;
using PowerLib.System.Validation;

namespace PowerLib.System;

public static class StringExtension
{
  private const int InvalidIndex = -1;

  private static int IndexExceptOfCore(string str, char[] exceptOf, int startIndex, int count)
  {
    for (var index = startIndex; count > 0; index++, count--)
      if (!exceptOf.Contains(str[index]))
        return index;
    return InvalidIndex;
  }

  public static int IndexExceptOf(this string str, char[] exceptOf)
  {
    Argument.That.NotNull(str);
    Argument.That.NotEmptyArray(exceptOf);

    return IndexExceptOfCore(str, exceptOf, 0, str.Length);
  }

  public static int IndexExceptOf(this string str, char[] exceptOf, int startIndex)
  {
    Argument.That.NotNull(str);
    Argument.That.NotEmptyArray(exceptOf);
    Argument.That.InRangeOut(str.Length, startIndex);

    return IndexExceptOfCore(str, exceptOf, startIndex, str.Length - startIndex);
  }

  public static int IndexExceptOf(this string str, char[] exceptOf, int startIndex, int count)
  {
    Argument.That.NotNull(str);
    Argument.That.NotEmptyArray(exceptOf);
    Argument.That.InRangeOut(str.Length, startIndex, count);

    return IndexExceptOfCore(str, exceptOf, startIndex, count);
  }

  private static int LastIndexExceptOfCore(string str, char[] exceptOf, int startIndex, int count)
  {
    for (var index = startIndex; count > 0; index--, count--)
      if (!exceptOf.Contains(str[index]))
        return index;
    return InvalidIndex;
  }

  public static int LastIndexExceptOf(this string str, char[] exceptOf)
  {
    Argument.That.NotNull(str);
    Argument.That.NotEmptyArray(exceptOf);

    return LastIndexExceptOfCore(str, exceptOf, str.Length == 0 ? 0 : str.Length - 1, str.Length);
  }

  public static int LastIndexExceptOf(this string str, char[] exceptOf, int startIndex)
  {
    Argument.That.NotNull(str);
    Argument.That.NotEmptyArray(exceptOf);
    Argument.That.InRangeOut(str.Length, startIndex);

    return LastIndexExceptOfCore(str, exceptOf, startIndex, startIndex + 1);
  }

  public static int LastIndexExceptOf(this string str, char[] exceptOf, int startIndex, int count)
  {
    Argument.That.NotNull(str);
    Argument.That.NotEmptyArray(exceptOf);
    Argument.That.InRangeRev(str.Length, startIndex, count);

    return LastIndexExceptOfCore(str, exceptOf, startIndex, count);
  }
}
