using System;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.NonGeneric.Extensions;

public static class ConverterExtension
{
  public static Converter<object?, object?> AsConverter(this IConverter converter)
    => Argument.That.NotNull(converter).Convert;
}
