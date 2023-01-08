using System;
using PowerLib.System.Validation;

namespace PowerLib.System.Collections.Generic.Extensions;

public static class ConverterExtension
{
  public static Converter<TIn?, TOut?> AsConverter<TIn, TOut>(this IConverter<TIn, TOut> converter)
    => Argument.That.NotNull(converter).Convert;
}
