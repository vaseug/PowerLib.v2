using System;

namespace PowerLib.System.Text;

public interface IParser<T>
{
  T Parse(string s, IFormatProvider formatProvider);

  bool TryParse(string s, IFormatProvider formatProvider, out T result);
}
