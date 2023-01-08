using System;
using PowerLib.System.Validation;

namespace PowerLib.System.Arrays;

/// <summary>
/// Represents inforamtion about regular long array dimension (length, lower and upper bound).
/// </summary>
/// <summary xml:lang="ru">
/// Предоставляет служебную информацию о размерности регулярного длинного массива (длина, нижняя и верхняя граница).
/// </summary>
public readonly struct ArrayLongDimension : IEquatable<ArrayLongDimension>
{
  private readonly long _length;
  private readonly long _lowerBound;

  #region Constructors

  public ArrayLongDimension(long length)
    : this(length, 0L)
  { }

  public ArrayLongDimension(long length, long lowerBound)
  {
    Argument.That.InLimitsOut(0L, length, lowerBound);

    _length = length;
    _lowerBound = lowerBound;
  }

  #endregion
  #region Properties

  public long Length
    => _length;

  public long LowerBound
    => _lowerBound;

  public long UpperBound
    => _lowerBound + _length - (_length == 0 ? 0L : 1L);

  #endregion
  #region Methods

  public bool Equals(ArrayLongDimension other)
    => other._lowerBound == _lowerBound && other._length == _length;

  public override bool Equals(object? obj)
    => obj is ArrayDimension arrayDimension && Equals(arrayDimension);

  public override int GetHashCode()
    => CompositeHashing.Default.GetHashCode(_lowerBound, _length);

  public static bool operator ==(ArrayLongDimension left, ArrayLongDimension right)
    => left.Equals(right);

  public static bool operator !=(ArrayLongDimension left, ArrayLongDimension right)
    => !left.Equals(right);

  #endregion
}
