using System;
using System.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Globalization;
using PowerLib.System.Reflection;
using PowerLib.System.Validation;

namespace PowerLib.System.ComponentModel;

/// <summary>
/// Contains information about enum item.
/// </summary>
public class EnumDescriptor : MemberDescriptor
{
  #region Constructors

  public EnumDescriptor(object value, Attribute[]? attributes)
    : this(Argument.That.NotNull(value).GetType().GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static)
      .Single(t => value.Equals(t.GetValue(null))), attributes, attributes is null)
  { }

  public EnumDescriptor(Type type, string name, bool ignoreCase, Attribute[]? attributes)
    : this(Argument.That.NotNull(type).GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static)
      .Single(t => string.Equals(name, t.Name, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture)), attributes, attributes is null)
  { }

  internal EnumDescriptor(object value)
    : base(Argument.That.NotNull(value).ToString() ?? string.Empty)
  {
    var type = value.GetType();
    Argument.That.IsValid(value, !type.IsEnum, "It is not enum type.");
    Value = value;
    UnderlyingValue = Convert.ChangeType(value, Enum.GetUnderlyingType(type), null);
    IsAtomic = true;
    originalAttributes = null;
  }

  internal EnumDescriptor(FieldInfo fieldInfo, Attribute[]? attributes, bool append)
    : base(Argument.That.NotNull(fieldInfo).Name)
  {
    Argument.That.IsValid(fieldInfo, fieldInfo.FieldType.IsEnum, "It is not enum type.");

    originalAttributes = ((attributes is null || append) ? fieldInfo.GetCustomAttributes() : Enumerable.Empty<Attribute>()).Concat(attributes ?? Enumerable.Empty<Attribute>()).ToArray();
    Value = fieldInfo.GetValue(null)!;
    UnderlyingValue = Argument.That.NotNull(Convert.ChangeType(fieldInfo.GetValue(null), Enum.GetUnderlyingType(fieldInfo.FieldType), null));
    IsAtomic = !EnumTypeDescriptor.IsFlags(fieldInfo.FieldType) || Convert.ToUInt64(Value, null) == 0UL ||
      (attributes?.OfType<AtomicEnumAttribute>().Any() ?? false) || (attributes is null || append) && Reflector.IsAttributeDefined<AtomicEnumAttribute>(fieldInfo);
  }

  #endregion
  #region Properties

  protected override Attribute[]? AttributeArray
    => GetOriginalAttributes();

  private readonly Attribute[]? originalAttributes;

  protected Attribute[]? GetOriginalAttributes()
    => originalAttributes;

  public object Value { get; }

  public object UnderlyingValue { get; }

  public bool IsAtomic { get; internal set; }

  public string ToString(string format, IFormatProvider provider)
    => format switch
    {
      "U" or "u" => DisplayName ?? Name,
      _ => ((IFormattable)Value).ToString(format, provider),
    };

  public override string ToString()
    => ToString("G", CultureInfo.CurrentCulture);

  #endregion
}
