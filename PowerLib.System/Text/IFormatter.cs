using System;

namespace PowerLib.System.Text;

public interface IFormatter<T>
{
  string Format(T value, string? format, IFormatProvider? formatProvider);
}
