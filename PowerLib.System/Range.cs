using System;
using System.Globalization;
using PowerLib.System.Validation;

namespace PowerLib.System;

public readonly struct Range : IFormattable, IEquatable<Range>
{
  private readonly int _index;
  private readonly int _count;

  #region Constructors

  public Range(int index, int count, bool reversed)
  {
    Argument.That.NonNegative(index);
    Argument.That.NonNegative(count);

    _index = index + (reversed ? count - (count == 0 ? 0 : 1) : 0);
    _count = count * (reversed ? -1 : 1);
  }

  public Range(int index, int count)
  {
    Argument.That.NonNegative(index);
    Argument.That.GreaterThanOrEqual(count, -index - 1);

    _index = index;
    _count = count;
  }

  #endregion
  #region Properties

  public int Index
    => _index;

  public int Count
    => _count < 0 ? -_count : _count;

  public int LowerBound
    => _count < 0 ? _index + _count + 1 : _index;

  public int UpperBound
    => _count < 0 ? _index : _index + _count - (_count == 0 ? 0 : 1);

  public int Step
    => _count < 0 ? -1 : 1;

  public bool Reversed
    => _count < 0;

  public (int lowerBound, int upperBound) Bounds
    => (LowerBound, UpperBound);

  public (int index, int count) Interval
    => (LowerBound, Count);

  #endregion
  #region Methods

  public static Range FromBounds((int lowerBound, int upperBound) bounds)
    => new Range(Argument.That.NonNegative(bounds.lowerBound), Argument.That.GreaterThanOrEqual(bounds.upperBound, bounds.lowerBound) - bounds.lowerBound + 1);

  public static Range FromBounds(int lowerBound, int upperBound)
    => new Range(Argument.That.NonNegative(lowerBound), Argument.That.GreaterThanOrEqual(upperBound, lowerBound) - lowerBound + 1);

  public static Range FromLowerBound(int lowerBound, int count)
    => new Range(Argument.That.Between(lowerBound, 0, int.MaxValue - 1), Argument.That.Between(count, 0, int.MaxValue - lowerBound));

  public static Range FromUpperBound(int upperBound, int count)
    => new Range(Argument.That.Between(upperBound, 0, int.MaxValue - 1), -Argument.That.Between(count, 0, upperBound + 1));

  public static Range FromIndex(int total, int index, bool reversed)
  {
    Argument.That.NonNegative(total);
    Argument.That.InRangeOut(total, index);

    return new Range(index, reversed ? index + 1 : total - index, reversed);
  }

  public static Range FromInterval((int index, int count) range)
    => new Range(range.index, range.count, false);

  public static Range Parse(string s)
    => Parse(s, NumberStyles.Integer, null);

  public static Range Parse(string s, NumberStyles styles)
    => Parse(s, styles, null);

  public static Range Parse(string s, IFormatProvider? formatProvider)
    => Parse(s, NumberStyles.Integer, formatProvider);

  public static Range Parse(string s, NumberStyles styles, IFormatProvider? formatProvider)
    => TryParse(s, styles, formatProvider, out var result) ? result : Format.That.Bad<Range>();

  public static bool TryParse(string s, out Range result)
    => TryParse(s, NumberStyles.Integer, null, out result);

  public static bool TryParse(string s, NumberStyles styles, out Range result)
    => TryParse(s, styles, null, out result);

  public static bool TryParse(string s, IFormatProvider? provider, out Range result)
    => TryParse(s, NumberStyles.Integer, provider, out result);

  public static bool TryParse(string s, NumberStyles styles, IFormatProvider? provider, out Range result)
    => RangeParseFormatInfo.Default.TryParse(s, styles, provider, out result);

  public Range Reverse()
    => new Range(_index + _count < 0 ? _count + 1 : _count - 1, -_count);

  public override string ToString()
    => RangeParseFormatInfo.Default.Format(this, null, null);

  public string ToString(string? format, IFormatProvider? formatProvider)
    => RangeParseFormatInfo.Default.Format(this, format, formatProvider);

  public bool Equals(Range other)
    => _index == other._index && _count == other._count;

  public override bool Equals(object? obj)
    => obj is Range value && Equals(value);

  public override int GetHashCode()
    => CompositeHashing.Default.GetHashCode(_index, _count);

  public static bool operator ==(Range left, Range right)
    => left.Equals(right);

  public static bool operator !=(Range left, Range right)
    => !left.Equals(right);

  #endregion
}
