using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PowerLib.System.Collections.Generic;
using PowerLib.System.Validation;

namespace PowerLib.System.ComponentModel
{
  /// <summary>
  /// Class to work with enum types.
  /// </summary>
  public static class EnumTypeDescriptor
	{
		#region Methods

    private static ulong ToUInt64(object value)
      => Type.GetTypeCode(value.GetType()) switch
      {
        TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 => unchecked((ulong)((IConvertible)value).ToInt64(null)),
        TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => ((IConvertible)value).ToUInt64(null),
        _ => Argument.That.Invalid(value),
      };

    public static bool IsFlags(Type type)
      => Argument.That.NotNull(type).IsEnum && type.IsDefined(typeof(FlagsAttribute));

    public static bool IsFlags<TEnum>()
      where TEnum : struct, IComparable, IConvertible, IFormattable
      => IsFlags(typeof(TEnum));

    public static object AllFlags(Type type)
      => Argument.That.IsValid(type, IsFlags(type))
        .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static)
        .Select(fi => ToUInt64(fi.GetValue(null)!))
        .Aggregate(0UL, (a, v) => a |= v, a => Enum.ToObject(type, a));

    public static TEnum AllFlags<TEnum>()
      where TEnum : struct, IComparable, IConvertible, IFormattable
      => (TEnum)AllFlags(typeof(TEnum));

    internal static EnumDescriptor[] GetEnumerations(Type type, IPredicate<EnumDescriptor>? predicate, EnumOptions options = EnumOptions.None)
    {
      Argument.That.NotNull(type);
      Argument.That.IsValid(type, type.IsEnum);

      var isFlags = type.IsDefined(typeof(FlagsAttribute));
      var enumDescriptors = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static)
        .Select(fieldInfo => new EnumDescriptor(fieldInfo, null, true))
        .Where(enumDescriptor => predicate is null || predicate.Match(enumDescriptor))
        .OrderBy(enumDescriptor => enumDescriptor.Value)
        .ToArray();
      if (isFlags)
      {
        int z = 0;
        while (z < enumDescriptors.Length && ToUInt64(enumDescriptors[z].Value) == 0)
          z++;
        for (int i = z; i < z + 1 && i < enumDescriptors.Length; i++)
          if (!enumDescriptors[i].IsAtomic)
            enumDescriptors[i].IsAtomic = true;
        for (int i = z + 1; i < enumDescriptors.Length; i++)
        {
          if (enumDescriptors[i].IsAtomic)
          {
            for (int j = i - 1; j >= 0 && Comparer.Default.Compare(enumDescriptors[i].Value, enumDescriptors[j].Value) == 0; j--)
              if (enumDescriptors[j].IsAtomic && !enumDescriptors[j].Attributes.OfType<AtomicEnumAttribute>().Any())
                enumDescriptors[j].IsAtomic = false;
          }
          else if (Comparer.Default.Compare(enumDescriptors[i].Value, enumDescriptors[i - 1].Value) == 0)
          {
            if (enumDescriptors[i - 1].IsAtomic && !enumDescriptors[i - 1].Attributes.OfType<AtomicEnumAttribute>().Any())
              enumDescriptors[i].IsAtomic = true;
          }
          else
          {
            ulong rFlags = 0, iFlags = ToUInt64(enumDescriptors[i].Value);
            for (int j = i - 1; j >= z; j--)
            {
              ulong jFlags = ToUInt64(enumDescriptors[j].Value);
              if ((iFlags & jFlags) == jFlags)
                rFlags |= jFlags;
            }
            if (iFlags != rFlags)
              enumDescriptors[i].IsAtomic = true;
          }
        }
        enumDescriptors = enumDescriptors
          .Where(enumDescriptor => (options & EnumOptions.SuppressComposite) == 0 || enumDescriptor.IsAtomic)
          .OrderBy(enumDescriptor => enumDescriptor.Value)
          .ToArray();
      }
      return enumDescriptors;
    }

    public static EnumDescriptor[] GetEnumerations(Type type, EnumOptions options = EnumOptions.None)
      => GetEnumerations(type, null, options);

    public static EnumDescriptor[] GetEnumerations<TEnum>(EnumOptions options = EnumOptions.None)
      where TEnum : struct, IComparable, IConvertible, IFormattable
      => GetEnumerations(typeof(TEnum), options);

		internal static EnumDescriptor[] GetEnumerations(object value, IPredicate<EnumDescriptor>? predicate, EnumOptions options = EnumOptions.None)
		{
      Argument.That.NotNull(value);
      Argument.That.Enum(value);

      var type = value.GetType();
      var isFlags = type.IsDefined(typeof(FlagsAttribute));
      var list = new List<EnumDescriptor>();
      var enumFlags = ToUInt64(value);
      var compFlags = 0UL;
      var equalAtomics = 0;
      foreach (var item in GetEnumerations(type, predicate, options)
        .Where(enumDescriptor => Comparer.Default.Compare(enumDescriptor.Value, value) <= 0)
        .OrderByDescending(enumDescriptor => enumDescriptor.Value))
      {
        ulong itemFlags = ToUInt64(item.Value);
        if (isFlags)
        {
          if (itemFlags == enumFlags && item.IsAtomic)
            equalAtomics++;
          else if (equalAtomics != 0 || itemFlags == 0)
            break;
          else if ((enumFlags & itemFlags) != itemFlags)
            continue;
          if ((options & EnumOptions.SuppressComposite) != 0 && !item.IsAtomic)
            continue;
          if ((options & EnumOptions.SuppressConstituent) != 0)
          {
            int j = 0;
            for (; j < list.Count && (list[j].IsAtomic || (ToUInt64(list[j].Value) & itemFlags) != itemFlags); j++) ;
            if (j < list.Count)
              continue;
          }
        }
        else if (itemFlags != enumFlags)
          break;
        compFlags |= itemFlags;
        list.Insert(0, item);
      }
      return enumFlags == compFlags ? list.ToArray() : Array.Empty<EnumDescriptor>();
		}

    public static EnumDescriptor[] GetEnumerations(object value, EnumOptions options = EnumOptions.None)
      => GetEnumerations(value, null, options);

    public static EnumDescriptor[] GetEnumerations<TEnum>(TEnum value, EnumOptions options = EnumOptions.None)
      where TEnum : struct, IComparable, IConvertible, IFormattable
      => GetEnumerations((object)value, options);

    #endregion
  }

  [Flags]
	public enum EnumOptions
	{
    None = 0x0,
		SuppressComposite = 0x1,
		SuppressConstituent = 0x2,
	}
}
	