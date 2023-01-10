using System;
using PowerLib.System.Collections.Matching;
using PowerLib.System.Validation;

namespace PowerLib.System;

/// <summary>
/// Represent enumeration extension methods.
/// </summary>
public static class PwrEnum
{
  #region Convert methods

  // Convert Byte to TEnum
  public static TEnum ToEnum<TEnum>(byte value)
    where TEnum : struct, Enum
    => (TEnum)Enum.ToObject(typeof(TEnum), value);

  // Convert UInt16 to TEnum
  public static TEnum ToEnum<TEnum>(ushort value)
    where TEnum : struct, Enum
    => (TEnum)Enum.ToObject(typeof(TEnum), value);
  
  // Convert UInt32 to TEnum
  public static TEnum ToEnum<TEnum>(uint value)
    where TEnum : struct, Enum
    => (TEnum)Enum.ToObject(typeof(TEnum), value);

  // Convert UInt64 to TEnum
  public static TEnum ToEnum<TEnum>(ulong value)
    where TEnum : struct, Enum
    => (TEnum)Enum.ToObject(typeof(TEnum), value);

  // Convert SByte to TEnum
  public static TEnum ToEnum<TEnum>(sbyte value)
    where TEnum : struct, Enum
    => (TEnum)Enum.ToObject(typeof(TEnum), value);

  // Convert Int16 to TEnum
  public static TEnum ToEnum<TEnum>(short value)
    where TEnum : struct, Enum
    => (TEnum)Enum.ToObject(typeof(TEnum), value);

  // Convert Int32 to TEnum
  public static TEnum ToEnum<TEnum>(int value)
    where TEnum : struct, Enum
    => (TEnum)Enum.ToObject(typeof(TEnum), value);

  // Convert Int64 to TEnum
  public static TEnum ToEnum<TEnum>(long value)
    where TEnum : struct, Enum
    => (TEnum)Enum.ToObject(typeof(TEnum), value);

  // Convert object to TEnum
  public static TEnum ToEnum<TEnum>(object value)
    where TEnum : struct, Enum
    => (TEnum)Enum.ToObject(typeof(TEnum), value);
  
  // Convert TEnum to value of underlying type
  public static object ToUnderlying<TEnum>(TEnum value)
    where TEnum : struct, Enum, IConvertible
    => value.GetTypeCode() switch
    {
      TypeCode.Byte => value.ToByte(null),
      TypeCode.UInt16 => value.ToUInt16(null),
      TypeCode.UInt32 => value.ToUInt32(null),
      TypeCode.UInt64 => value.ToUInt64(null),
      TypeCode.SByte => value.ToSByte(null),
      TypeCode.Int16 => value.ToInt16(null),
      TypeCode.Int32 => value.ToInt32(null),
      TypeCode.Int64 => value.ToInt64(null),
      _ => Operation.That.Failed()
    };

  /// <summary>
  /// Parse TEnum flags from separated string.
  /// If item prefixed with '+' then flag added to value,
  /// if item prefixed with '-' then flag removed from value,
  /// if item prefixed with '!' then flag inverted in value.
  /// </summary>
  /// <typeparam name="TEnum"></typeparam>
  /// <param name="value"></param>
  /// <param name="input"></param>
  /// <param name="ignoreCase"></param>
  /// <param name="separators"></param>
  /// <returns></returns>
  public static TEnum ParseFlags<TEnum>(this TEnum value, string input, bool ignoreCase, params char[] separators)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
  {
    Argument.That.EnumFlags(value);
    Argument.That.NotNull(input);
    Argument.That.NotEmpty(separators);

    Type type = typeof(TEnum);
    ulong number = value.ToUInt64(null);
    string[] items = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < items.Length; i++)
    {
      items[i] = items[i].Trim();
      var delta = items[i][0] switch
      {
        '+' or '-' or '!' => ((TEnum)Enum.Parse(type,
#if NETCOREAPP2_1_OR_GREATER
        items[i].AsSpan(1),
#else
        items[i].Substring(1),
#endif
        ignoreCase)).ToUInt64(null),
        _ => ((TEnum)Enum.Parse(type, items[i], ignoreCase)).ToUInt64(null),
      };
      switch (items[i][0])
      {
        case '-':
          number &= ~delta;
          break;
        case '!':
          number = (number | delta) & (number ^ delta);
          break;
        default:
          number |= delta;
          break;
      }
    }
    return (TEnum)Enum.ToObject(type, number);
  }

  #endregion
  #region Flags operation methods

  /// <summary>
  /// Combine flags enumeration values (Bitwise OR operation).
  /// </summary>
  /// <typeparam name="TEnum">Type of enumeration.</typeparam>
  /// <param name="value">First value to combine.</param>
  /// <param name="flags">Other values to combine.</param>
  /// <returns>Combined value.</returns>
  public static TEnum CombineFlags<TEnum>(this TEnum value, params TEnum[] flags)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
  {
    switch (value.GetTypeCode())
    {
      case TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64:
        var uResult = Argument.That.EnumFlags(value).ToUInt64(null);
        if (flags is not null)
          for (int i = 0; i < flags.Length; i++)
            uResult |= flags[i].ToUInt64(null);
        return (TEnum)Enum.ToObject(typeof(TEnum), uResult);
      case TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64:
        var sResult = Argument.That.EnumFlags(value).ToInt64(null);
        if (flags is not null)
          for (int i = 0; i < flags.Length; i++)
            sResult |= flags[i].ToInt64(null);
        return (TEnum)Enum.ToObject(typeof(TEnum), sResult);
      default:
        return Operation.That.Failed();
    }
  }

  /// <summary>
  /// Overlap flags enumeration values (Bitwise AND operation).
  /// </summary>
  /// <typeparam name="TEnum">Type of enumeration.</typeparam>
  /// <param name="value">First value to overlap.</param>
  /// <param name="flags">Other values to overlap.</param>
  /// <returns>Overlapped value.</returns>
  public static TEnum OverlapFlags<TEnum>(this TEnum value, params TEnum[] flags)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
  {
    switch (value.GetTypeCode())
    {
      case TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64:
        var uresult = Argument.That.EnumFlags(value).ToUInt64(null);
        if (flags is not null)
          for (int i = 0; i < flags.Length; i++)
            uresult &= flags[i].ToUInt64(null);
        return (TEnum)Enum.ToObject(typeof(TEnum), uresult);
      case TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64:
        var sresult = Argument.That.EnumFlags(value).ToInt64(null);
        if (flags is not null)
          for (int i = 0; i < flags.Length; i++)
            sresult &= flags[i].ToInt64(null);
        return (TEnum)Enum.ToObject(typeof(TEnum), sresult);
      default:
        return Operation.That.Failed();
    }
  }

  /// <summary>
  /// Remove flags enumeration <paramref name="flags"/> from <paramref name="value"/>
  /// </summary>
  /// <typeparam name="TEnum"></typeparam>
  /// <param name="value"></param>
  /// <param name="flags"></param>
  /// <returns></returns>
  public static TEnum RemoveFlags<TEnum>(this TEnum value, params TEnum[] flags)
      where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
  {
    switch (value.GetTypeCode())
    {
      case TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64:
        var uresult = Argument.That.EnumFlags(value).ToUInt64(null);
        if (flags is not null)
          for (int i = 0; i < flags.Length && uresult != 0UL; i++)
            uresult &= ~flags[i].ToUInt64(null);
        return (TEnum)Enum.ToObject(typeof(TEnum), uresult);
      case TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64:
        var sresult = Argument.That.EnumFlags(value).ToInt64(null);
        if (flags is not null)
          for (int i = 0; i < flags.Length && sresult != 0; i++)
            sresult &= ~flags[i].ToInt64(null);
        return (TEnum)Enum.ToObject(typeof(TEnum), sresult);
      default:
        return Operation.That.Failed();
    }
  }

  /// <summary>
  /// Inverse flags enumeration value.
  /// </summary>
  /// <typeparam name="TEnum">Type of enumeration.</typeparam>
  /// <param name="value">Flags enumeration value to inverse.</param>
  /// <param name="mask">Flags enumeration mask for inverse.</param>
  /// <returns>Inversed flags enumeration value.</returns>
  public static TEnum InverseFlags<TEnum>(this TEnum value, TEnum mask)
      where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
  {
    return value.GetTypeCode() switch
    {
      TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => (TEnum)Enum.ToObject(typeof(TEnum), Argument.That.EnumFlags(value).ToUInt64(null) ^ mask.ToUInt64(null)),
      TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 => (TEnum)Enum.ToObject(typeof(TEnum), Argument.That.EnumFlags(value).ToInt64(null) ^ mask.ToInt64(null)),
      _ => Operation.That.Failed(),
    };
  }

  /// <summary>
  /// Match two flags enumeration.
  /// </summary>
  /// <typeparam name="TEnum">Type of enumeration</typeparam>
  /// <param name="xValue">First enumeration value</param>
  /// <param name="yValue">Second enumeration value</param>
  /// <returns></returns>
  public static FlagsMatchResult MatchFlags<TEnum>(this TEnum xValue, TEnum yValue)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
  {
    switch (Type.GetTypeCode(typeof(TEnum)))
    {
      case TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64:
        var xuNumber = xValue.ToUInt64(null);
        var yuNumber = yValue.ToUInt64(null);
        if (xuNumber == yuNumber)
          return FlagsMatchResult.Equal;
        var uNumber = xuNumber & yuNumber;
        if (uNumber == xuNumber)
          return FlagsMatchResult.Belong;
        else if (uNumber == yuNumber)
          return FlagsMatchResult.Enclose;
        else if (uNumber != 0UL)
          return FlagsMatchResult.Overlap;
        else
          return FlagsMatchResult.NonOverlap;
      case TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64:
        var xsNumber = xValue.ToInt64(null);
        var ysNumber = yValue.ToInt64(null);
        if (xsNumber == ysNumber)
          return FlagsMatchResult.Equal;
        var sNumber = xsNumber & ysNumber;
        if (sNumber == xsNumber)
          return FlagsMatchResult.Belong;
        else if (sNumber == ysNumber)
          return FlagsMatchResult.Enclose;
        else if (sNumber != 0L)
          return FlagsMatchResult.Overlap;
        else
          return FlagsMatchResult.NonOverlap;
      default:
        return Operation.That.Failed();
    }
  }

  /// <summary>
  /// Determines whether the values of the flags parameter are set in the value.
  /// </summary>
  /// <typeparam name="TEnum"></typeparam>
  /// <param name="value"></param>
  /// <param name="flags"></param>
  /// <returns></returns>
  public static bool IsFlagsSet<TEnum>(this TEnum value, TEnum flags)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
  {
    switch (Type.GetTypeCode(typeof(TEnum)))
    {
      case TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64:
        var uValue = value.ToUInt64(null);
        var uFlags = flags.ToUInt64(null);
        return (uValue & uFlags) == uFlags;
      case TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64:
        var sValue = value.ToInt64(null);
        var sFlags = flags.ToInt64(null);
        return (sValue & sFlags) == sFlags;
      default:
        return Operation.That.Failed();
    }
  }

  /// <summary>
  /// Determines whether the values of the flags parameter are overlapped with the value.
  /// </summary>
  /// <typeparam name="TEnum"></typeparam>
  /// <param name="value"></param>
  /// <param name="flags"></param>
  /// <returns></returns>
  public static bool IsFlagsOverlapped<TEnum>(this TEnum value, TEnum flags)
    where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
  {
    switch (Type.GetTypeCode(typeof(TEnum)))
    {
      case TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64:
        var uValue = value.ToUInt64(null);
        var uFlags = flags.ToUInt64(null);
        return (uValue & uFlags) != 0;
      case TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64:
        var sValue = value.ToInt64(null);
        var sFlags = flags.ToInt64(null);
        return (sValue & sFlags) != 0;
      default:
        return Operation.That.Failed();
    }
  }

  #endregion
}
