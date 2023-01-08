namespace PowerLib.System.Collections.Generic;

/// <summary>
/// Represents interface to convert input.
/// </summary>
/// <typeparam name="T">Type of matched object.</typeparam>
public interface IConverter<in TIn, out TOut>
{
  /// <summary>
  /// </summary>
  /// <param name="value ">Value to convert.</param>
  /// <returns></returns>
  TOut? Convert(TIn? value);
}
