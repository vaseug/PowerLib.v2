using System;
using PowerLib.System.Validation;

namespace PowerLib.System.Arrays;

/// <summary>
/// Represents inforamtion about regular array dimension (length, lower and upper bound).
/// </summary>
/// <summary xml:lang="ru">
/// Предоставляет служебную информацию о размерности регулярного массива (длина, нижняя и верхняя граница).
/// </summary>
public readonly struct ArrayDimension : IEquatable<ArrayDimension>
{
  private readonly int _length;
  private readonly int _lowerBound;

  #region Constructors

  public ArrayDimension(int length)
    : this(length, 0)
  { }

  public ArrayDimension(int length, int lowerBound)
  {
    Argument.That.InLimitsOut(0, length, lowerBound);

    _length = length;
    _lowerBound = lowerBound;
  }

  #endregion
  #region Properties

  public int Length
    => _length;

  public int LowerBound
    => _lowerBound;

  public int UpperBound
    => _lowerBound + _length - (_length == 0 ? 0 : 1);

  #endregion
  #region Methods

  public bool Equals(ArrayDimension other)
    => other._lowerBound == _lowerBound && other._length == _length;

  public override bool Equals(object? obj)
    => obj is ArrayDimension arrayDimension && Equals(arrayDimension);

  public override int GetHashCode()
    => CompositeHashing.Default.GetHashCode(_lowerBound, _length);

  public static bool operator==(ArrayDimension left, ArrayDimension right)
    => left.Equals(right);

  public static bool operator!=(ArrayDimension left, ArrayDimension right)
    => !left.Equals(right);

  #endregion
}
