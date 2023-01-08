using System;
using System.Globalization;
using PowerLib.System.Validation;

namespace PowerLib.System;

public readonly struct LongRange : IFormattable, IEquatable<LongRange>
{
  private readonly long _index;
  private readonly long _count;

  #region Constructors

  public LongRange(long index, long count, bool reversed)
  {
    Argument.That.NonNegative(index);
    Argument.That.NonNegative(count);

    _index = index + (reversed ? count - (count == 0L ? 0L : 1L) : 0L);
    _count = count * (reversed ? -1L : 1L);
  }

  public LongRange(long index, long count)
  {
    Argument.That.NonNegative(index);
    Argument.That.GreaterThanOrEqual(count, -index - 1L);

    _index = index;
    _count = count;
  }

  #endregion
  #region Properties

  public long Index
    => _index;

  public long Count
    => _count < 0L ? -_count : _count;

  public long LowerBound
    => _count < 0L ? _index + _count + 1L : _index;

  public long UpperBound
    => _count < 0L ? _index : _index + _count - (_count == 0L ? 0L : 1L);

  public long Step
    => _count < 0L ? -1L : 1L;

  public bool Reversed
    => _count < 0L;

  #endregion
  #region Methods

  public static LongRange FromBounds(long lowerBound, long upperBound)
    => new LongRange(Argument.That.NonNegative(lowerBound), Argument.That.GreaterThanOrEqual(upperBound, lowerBound) - lowerBound + 1L);

  public static LongRange FromLowerBound(long lowerBound, long count)
    => new LongRange(Argument.That.Between(lowerBound, 0L, long.MaxValue - 1L), Argument.That.Between(count, 0L, long.MaxValue - lowerBound));

  public static LongRange FromUpperBound(long upperBound, long count)
    => new LongRange(Argument.That.Between(upperBound, 0L, long.MaxValue - 1L), -Argument.That.Between(count, 0L, upperBound + 1L));

  public static LongRange FromIndex(long total, long index, bool reversed)
  {
    Argument.That.NonNegative(total);
    Argument.That.InRangeOut(total, index);

    return new LongRange(index, reversed ? index + 1L : total - index, reversed);
  }

  public static LongRange FromRange(LongRange range, bool reversed)
    => reversed ? range.Reverse() : range;

  public static LongRange Parse(string s)
    => Parse(s, NumberStyles.Integer, null);

  public static LongRange Parse(string s, NumberStyles styles)
    => Parse(s, styles, null);

  public static LongRange Parse(string s, IFormatProvider? formatProvider)
    => Parse(s, NumberStyles.Integer, formatProvider);

  public static LongRange Parse(string s, NumberStyles styles, IFormatProvider? formatProvider)
    => TryParse(s, styles, formatProvider, out var result) ? result : Format.That.Bad<LongRange>();

  public static bool TryParse(string s, out LongRange result)
    => TryParse(s, NumberStyles.Integer, null, out result);

  public static bool TryParse(string s, NumberStyles styles, out LongRange result)
    => TryParse(s, styles, null, out result);

  public static bool TryParse(string s, IFormatProvider provider, out LongRange result)
    => TryParse(s, NumberStyles.Integer, provider, out result);

  public static bool TryParse(string s, NumberStyles styles, IFormatProvider? provider, out LongRange result)
    => LongRangeParseFormatInfo.Default.TryParse(s, styles, provider, out result);

  public LongRange Reverse()
    => new LongRange(_index + _count < 0L ? _count + 1L : _count - 1L, -_count);

  public override string ToString()
    => LongRangeParseFormatInfo.Default.Format(this, null, null);

  public string ToString(string? format, IFormatProvider? formatProvider)
    => LongRangeParseFormatInfo.Default.Format(this, format, formatProvider);

  public bool Equals(LongRange other)
    => _index == other._index && _count == other._count;

  public override bool Equals(object? obj)
    => obj is Range value && Equals(value);

  public override int GetHashCode()
    => CompositeHashing.Default.GetHashCode(_index, _count);

  public static bool operator ==(LongRange left, LongRange right)
    => left.Equals(right);

  public static bool operator !=(LongRange left, LongRange right)
    => !left.Equals(right);

  #endregion
}
