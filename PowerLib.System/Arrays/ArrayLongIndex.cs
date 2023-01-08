using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using PowerLib.System.Validation;

namespace PowerLib.System.Arrays;

/// <summary>
///	ArrayIndex is allow addressing any dimensional arrays even with non zero lower boundaries.
/// ArrayIndex contains consistent values of flat index and dimensional indices.
/// </summary>
/// <summary xml:lang="ru">
/// ArrayIndex позволяет производить адресацию массивов любой размерности, даже с ненулевой нижней границей.
/// ArrayIndex содержит согласованные значения сквозного индекса и индексов каждой из размерности массива.
/// </summary>
public sealed class ArrayLongIndex
{
  private readonly ArrayLongInfo _arrayInfo;
  private bool _asRanges;
  private bool _zeroBased;
  private bool _checkOut;
  private long _carry;
  private long _flatIndex;
  private readonly long[] _dimIndices;
  private IReadOnlyList<long>? _dimIndicesAccessor;

  #region Constructors

  /// <summary>
  /// Construct array index
  /// </summary>
  /// <param name="arrayInfo">Array information class.</param>
  public ArrayLongIndex(ArrayLongInfo arrayInfo)
    : this(arrayInfo, Bound.Lower)
  { }

  /// <summary>
  /// Construct array index
  /// </summary>
  /// <param name="arrayInfo">Array information class.</param>
  /// <param name="bound">If truthen.</param>
  public ArrayLongIndex(ArrayLongInfo arrayInfo, Bound bound)
  {
    _arrayInfo = arrayInfo;
    _dimIndices = new long[_arrayInfo.Rank];
    switch (bound)
    {
      case Bound.Lower:
        _flatIndex = 0L;
        _arrayInfo.GetMinDimIndices(_dimIndices);
        break;
      case Bound.Upper:
        _flatIndex = _arrayInfo.Length == 0L ? 0L : _arrayInfo.Length - 1L;
        _arrayInfo.GetMaxDimIndices(_dimIndices);
        break;
      default:
        Operation.That.Failed();
        break;
    }
  }

  #endregion
  #region Properties

  /// <summary>
  /// 
  /// </summary>
  public bool AsRanges
  {
    get => _asRanges;
    set => _asRanges = value;
  }

  /// <summary>
  /// Dimensional indices would be represented as zerbase(if its nones).
  /// </summary>
  public bool ZeroBased
  {
    get => _zeroBased;
    set
    {
      if (_arrayInfo.Length == 0L)
        _arrayInfo.GetMinDimIndices(value, _dimIndices);
      else
        _arrayInfo.CalcDimIndices(_flatIndex, value, _dimIndices);
      _zeroBased = value;
    }
  }

  /// <summary>
  /// Check out range of dimensional index
  /// </summary>
  public bool CheckOut
  {
    get => _checkOut;
    set => _checkOut = value;
  }

  /// <summary>
  /// Carry value from previous flat index operation
  /// </summary>
  public long Carry
    => _carry;

  /// <summary>
  /// Array info
  /// </summary>
  public ArrayLongInfo ArrayInfo
    => _arrayInfo;

  public long FlatIndex
  {
    get => _flatIndex;
    set
    {
      if (value == FlatIndex)
        return;
      _arrayInfo.CalcDimIndices(value, _zeroBased, _dimIndices);
      _flatIndex = value;
      _carry = 0;
    }
  }

  public IReadOnlyList<long> DimIndices
    => LazyInitializer.EnsureInitialized(ref _dimIndicesAccessor, () => new ReadOnlyCollection<long>(_dimIndices));

  #endregion
  #region Methods
  #region Internal methods

  /// <summary>
  /// Adds value to the value of the flat delta index. 
  /// If the value of property Module false and indices out of range then throwing an exception.
  /// Otherwise, the addition is performed by module the length of the array and exception is not throw.
  /// </summary>
  /// <param name="delta"></param>
  /// <returns></returns>
  private long AddFlatIndexCore(long delta)
  {
    long carry = 0;
    bool checkOut = _checkOut;
    if (!checkOut && (delta > _arrayInfo.Length || delta < -_arrayInfo.Length))
      delta %= _arrayInfo.Length;
    else
      Operation.That.IsInRange(_arrayInfo.Length > 0);
    if (delta > 0 && _arrayInfo.Length - _flatIndex < delta + 1)
    {
      Operation.That.IsInRange(!checkOut);
      carry = delta - (_arrayInfo.Length - _flatIndex - 1);
    }
    else if (delta < 0 && _flatIndex < -delta)
    {
      Operation.That.IsInRange(!checkOut);
      carry = -delta - _flatIndex;
    }
    _flatIndex +=
      delta > 0 && _arrayInfo.Length - _flatIndex < delta + 1 ? delta - _arrayInfo.Length :
      delta < 0 && _flatIndex < -delta ? delta + _arrayInfo.Length :
      delta;
    if (delta == 1)
      _arrayInfo.IncDimIndices(_zeroBased, _dimIndices);
    else if (delta == -1)
      _arrayInfo.DecDimIndices(_zeroBased, _dimIndices);
    else
      _arrayInfo.CalcDimIndices(_flatIndex, _zeroBased, _dimIndices);
    return _carry = carry;
  }

  #endregion
  #region Public methods

  public object? GetValue(Array array)
    => _arrayInfo.GetValue(array, _asRanges, _zeroBased, _dimIndices);

  public void SetValue(Array array, object? value)
    => _arrayInfo.SetValue(array, value, _asRanges, _zeroBased, _dimIndices);

  public T GetValue<T>(Array array)
    => _arrayInfo.GetValue<T>(array, _asRanges, _zeroBased, _dimIndices);

  public void SetValue<T>(Array array, T value)
    => _arrayInfo.SetValue(array, value, _asRanges, _zeroBased, _dimIndices);

  public void GetDimIndices(long[] dimIndices)
  {
    Argument.That.ExactCount(dimIndices, ArrayInfo.Rank);

    _dimIndices.CopyTo(dimIndices, 0);
  }

  public void SetDimIndices(params long[] dimIndices)
  {
    Argument.That.ExactCount(dimIndices, ArrayInfo.Rank);

    _flatIndex = _arrayInfo.CalcFlatIndex(dimIndices);
    _carry = 0;
    dimIndices.CopyTo(_dimIndices, 0);
  }

  public long GetLongDimIndex(int dimension)
  {
    Argument.That.InRangeIn(_dimIndices, dimension);

    return _dimIndices[dimension];
  }

  public long Add(long delta)
    => AddFlatIndexCore(delta);

  public long Sub(long delta)
    => AddFlatIndexCore(-delta);

  public bool Inc()
    => AddFlatIndexCore(1) != 0;

  public bool Dec()
    => AddFlatIndexCore(-1) != 0;

  public void SetMin()
  {
    _carry = 0;
    _flatIndex = 0;
    _arrayInfo.GetMinDimIndices(_zeroBased, _dimIndices);
  }

  public void SetMax()
  {
    _carry = 0;
    _flatIndex = _arrayInfo.Length == 0 ? 0 : _arrayInfo.Length - 1;
    _arrayInfo.GetMaxDimIndices(_zeroBased, _dimIndices);
  }

  public bool IsMin
    => _arrayInfo.Length > 0 && _flatIndex == 0;

  public bool IsMax
    => _arrayInfo.Length > 0 && _flatIndex == _arrayInfo.Length - 1;

  #endregion
  #endregion
  #region Operators

  /// <summary>
  /// Conversation operator to flat index value.
  /// </summary>
  /// <param name="arrayIndex">Array index.</param>
  /// <returns>Flaindex value.</returns>
  public static implicit operator long(ArrayLongIndex arrayIndex)
  {
    Argument.That.NotNull(arrayIndex);

    return arrayIndex.FlatIndex;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="arrayIndex"></param>
  /// <returns></returns>
  public static ArrayLongIndex operator ++(ArrayLongIndex arrayIndex)
  {
    Argument.That.NotNull(arrayIndex);

    arrayIndex.Inc();
    return arrayIndex;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="arrayIndex"></param>
  /// <returns></returns>
  public static ArrayLongIndex operator --(ArrayLongIndex arrayIndex)
  {
    Argument.That.NotNull(arrayIndex);

    arrayIndex.Dec();
    return arrayIndex;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="arrayIndex"></param>
  /// <param name="delta"></param>
  /// <returns></returns>
  public static ArrayLongIndex operator +(ArrayLongIndex arrayIndex, long delta)
  {
    Argument.That.NotNull(arrayIndex);

    arrayIndex.Add(delta);
    return arrayIndex;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="arrayIndex"></param>
  /// <param name="delta"></param>
  /// <returns></returns>
  public static ArrayLongIndex operator -(ArrayLongIndex arrayIndex, long delta)
  {
    Argument.That.NotNull(arrayIndex);

    arrayIndex.Sub(delta);
    return arrayIndex;
  }

  #endregion
}
